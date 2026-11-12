using BeafSoup.Audio.Generation;
using BeafSoup.Content.EntityFrameworkCore;
using BeafSoup.Exercises.EntityFrameworkCore;
using BeafSoup.Localization.EntityFrameworkCore;
using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.DbMigrator;

/// <summary>种子入库管线：加载 data/seed → 领域校验 → 显式映射为持久化行。空库插入；非空库仅在 replaceExisting 时先清空内容表。</summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(string connectionString, string seedDirectory, bool replaceExisting)
    {
        var seed = SeedLoader.Load(seedDirectory);
        var result = SeedValidator.Validate(seed);
        if (!result.IsValid)
            throw new InvalidDataException($"种子数据校验失败：{string.Join("；", result.Errors)}");
        var validated = result.Value!;

        await MigrationRunner.MigrateAsync(connectionString);

        await using (var probe = new LocalizationDbContext(
            MigrationRunner.CreateOptions<LocalizationDbContext>(connectionString, "__EFMigrationsHistory_Localization")))
        {
            var hasData = await probe.LocalizedTexts.AnyAsync();
            if (hasData && !replaceExisting)
            {
                Console.WriteLine("数据库已含内容数据，跳过种子填充（使用 seed --force 替换或 reset 重建）。");
                return;
            }
        }

        if (replaceExisting)
            await ClearContentTablesAsync(connectionString);

        var now = DateTime.UtcNow;

        // 1) 本地化：语义键注册表 + 本地化文本 + 音色角色配置
        await using (var context = new LocalizationDbContext(
            MigrationRunner.CreateOptions<LocalizationDbContext>(connectionString, "__EFMigrationsHistory_Localization")))
        {
            // 概念键注册为语义键，但词汇域不维护 localized_texts——概念解释走词典 K-V 缓存中的基础语言主词元
            var vocabularyKeys = validated.Concepts.Select(c => c.ConceptKey).Distinct();
            var contentKeys = validated.Sentences.Select(s => s.Key)
                .Concat(validated.Courses.Select(c => c.TitleKey))
                .Concat(validated.Courses.SelectMany(c => c.Units.Select(u => u.TitleKey)))
                .Concat(validated.Courses.SelectMany(c => c.Units.SelectMany(u => u.Lessons.Select(l => l.TitleKey))))
                .Distinct();

            context.SemanticKeys.AddRange(vocabularyKeys
                .Select(k => LocalizationPersistenceMapper.ToRow(k, "vocabulary", now))
                .Concat(contentKeys.Select(k => LocalizationPersistenceMapper.ToRow(k, "content", now)))
                .Concat(validated.UiKeys.Select(k => LocalizationPersistenceMapper.ToRow(k, "ui", now))));
            context.LocalizedTexts.AddRange(validated.LocalizedTexts.Select(LocalizationPersistenceMapper.ToRow));
            context.VoiceRoleConfigs.AddRange(validated.VoiceRoles.Select(LocalizationPersistenceMapper.ToRow));
            await context.SaveChangesAsync();
        }

        // 2) 词汇：概念（级联词元/标签/范式槽位）
        await using (var context = new VocabularyDbContext(
            MigrationRunner.CreateOptions<VocabularyDbContext>(connectionString, "__EFMigrationsHistory_Vocabulary")))
        {
            context.LexicalConcepts.AddRange(validated.Concepts.Select(VocabularyPersistenceMapper.ToRow));
            await context.SaveChangesAsync();
        }

        // 3) 内容：句子、对话、课程树（含课时习题引用）
        await using (var context = new ContentDbContext(
            MigrationRunner.CreateOptions<ContentDbContext>(connectionString, "__EFMigrationsHistory_Content")))
        {
            context.Sentences.AddRange(validated.Sentences.Select(ContentPersistenceMapper.ToRow));
            context.Dialogues.AddRange(validated.Dialogues.Select(ContentPersistenceMapper.ToRow));
            context.Courses.AddRange(validated.Courses.Select(ContentPersistenceMapper.ToRow));
            await context.SaveChangesAsync();
        }

        // 4) 习题：版本化负载实例（发布前已经注册表全量校验）
        await using (var context = new ExercisesDbContext(
            MigrationRunner.CreateOptions<ExercisesDbContext>(connectionString, "__EFMigrationsHistory_Exercises")))
        {
            context.ExerciseInstances.AddRange(validated.Exercises.Select(ToRow));
            await context.SaveChangesAsync();
        }

        Console.WriteLine($"种子填充完成：概念 {validated.Concepts.Count}，句子 {validated.Sentences.Count}，" +
            $"对话 {validated.Dialogues.Count}，课程 {validated.Courses.Count}，习题 {validated.Exercises.Count}，" +
            $"本地化文本 {validated.LocalizedTexts.Count}。");
    }

    /// <summary>
    /// 仅同步本地化数据（界面文案语义键、文本与音色配置），不清空课程与学习数据；
    /// 用于种子文案更新后的幂等回填，避免重新 seed --force 波及课程表。
    /// </summary>
    public static async Task SyncLocalizationAsync(string connectionString, string seedDirectory)
    {
        var seed = SeedLoader.Load(seedDirectory);
        var result = SeedValidator.Validate(seed);
        if (!result.IsValid)
            throw new InvalidDataException($"种子数据校验失败：{string.Join("；", result.Errors)}");
        var validated = result.Value!;

        await MigrationRunner.MigrateAsync(connectionString);
        await using var context = new LocalizationDbContext(
            MigrationRunner.CreateOptions<LocalizationDbContext>(connectionString, "__EFMigrationsHistory_Localization"));
        await using var transaction = await context.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        var uiKeys = validated.UiKeys.Select(key => key.Value).ToHashSet(StringComparer.Ordinal);

        // 语义键：只补 UI 域；已存在的键保持创建时间，不重复插入
        var existingKeys = await context.SemanticKeys.ToDictionaryAsync(row => row.Key, StringComparer.Ordinal);
        var addedKeys = 0;
        foreach (var key in validated.UiKeys)
        {
            if (existingKeys.TryGetValue(key.Value, out var row))
            {
                row.Domain = "ui";
                continue;
            }
            context.SemanticKeys.Add(LocalizationPersistenceMapper.ToRow(key, "ui", now));
            addedKeys++;
        }

        // 文本：按 (key, language) 更新或插入；仅处理 ui 域语义键，不触碰课程/句子文本
        var existingTexts = await context.LocalizedTexts.ToDictionaryAsync(row => (row.Key, row.LanguageCode));
        var addedTexts = 0;
        var updatedTexts = 0;
        foreach (var text in validated.LocalizedTexts.Where(text => uiKeys.Contains(text.Key.Value)))
        {
            if (existingTexts.TryGetValue((text.Key.Value, text.Language.Value), out var row))
            {
                if (row.Text == text.Text && row.Revision == text.Revision)
                    continue;
                // 文本更新必须递增修订号，不回退到种子文件的初始修订
                row.Text = text.Text;
                row.Revision = Math.Max(row.Revision + 1, text.Revision);
                updatedTexts++;
                continue;
            }
            context.LocalizedTexts.Add(LocalizationPersistenceMapper.ToRow(text));
            addedTexts++;
        }

        // 音色配置：角色映射变化时覆盖，缺失时补齐
        var existingVoices = await context.VoiceRoleConfigs.ToDictionaryAsync(row => (row.LanguageCode, row.Role));
        var writtenVoices = 0;
        foreach (var voice in validated.VoiceRoles)
        {
            if (existingVoices.TryGetValue((voice.Language.Value, voice.Role.ToString()), out var row))
            {
                if (row.AssetFileToken == voice.AssetFileToken && row.TtsVoiceName == voice.TtsVoiceName)
                    continue;
                row.AssetFileToken = voice.AssetFileToken;
                row.TtsVoiceName = voice.TtsVoiceName;
                writtenVoices++;
                continue;
            }
            context.VoiceRoleConfigs.Add(LocalizationPersistenceMapper.ToRow(voice));
            writtenVoices++;
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine($"本地化同步完成：新增语义键 {addedKeys}，新增文本 {addedTexts}，" +
            $"更新文本 {updatedTexts}，写入音色配置 {writtenVoices}。");
    }

    private static BeafSoup.Exercises.EntityFrameworkCore.ExerciseInstanceRow ToRow(BeafSoup.Exercises.ExerciseInstance instance)
        => new()
        {
            Id = instance.Id.ToString(),
            TypeKey = instance.TypeKey,
            PayloadVersion = instance.PayloadVersion,
            PayloadJson = instance.PayloadJson,
            Difficulty = instance.Difficulty,
            HintKey = instance.HintKey,
        };

    private static async Task ClearContentTablesAsync(string connectionString)
    {
        // 外键约束下按依赖顺序清空；学习/账户数据不在这些表中（第 3、4 阶段）
        string[] tables =
        [
            "lesson_exercises", "exercise_instances", "lessons", "skill_units", "courses",
            "essay_sentences", "essays", "dialogue_lines", "dialogues", "sentence_span_links",
            "sentence_tokens", "sentence_tokenizations", "sentences",
            "paradigm_cells", "lexeme_tags", "lexemes", "lexical_concepts",
            "voice_role_configs", "localized_texts", "semantic_keys",
        ];
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {table};";
            command.Transaction = (SqliteTransaction)transaction;
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        Console.WriteLine("已清空内容数据表。");
    }
}
