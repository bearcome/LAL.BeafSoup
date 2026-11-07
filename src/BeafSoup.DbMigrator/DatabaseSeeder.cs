using BeafSoup.Audio.Generation;
using BeafSoup.Content.EntityFrameworkCore;
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
                .Concat(contentKeys.Select(k => LocalizationPersistenceMapper.ToRow(k, "content", now))));
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

        // 3) 内容：句子、对话、课程树
        await using (var context = new ContentDbContext(
            MigrationRunner.CreateOptions<ContentDbContext>(connectionString, "__EFMigrationsHistory_Content")))
        {
            context.Sentences.AddRange(validated.Sentences.Select(ContentPersistenceMapper.ToRow));
            context.Dialogues.AddRange(validated.Dialogues.Select(ContentPersistenceMapper.ToRow));
            context.Courses.AddRange(validated.Courses.Select(ContentPersistenceMapper.ToRow));
            await context.SaveChangesAsync();
        }

        Console.WriteLine($"种子填充完成：概念 {validated.Concepts.Count}，句子 {validated.Sentences.Count}，" +
            $"对话 {validated.Dialogues.Count}，课程 {validated.Courses.Count}，本地化文本 {validated.LocalizedTexts.Count}。");
    }

    private static async Task ClearContentTablesAsync(string connectionString)
    {
        // 外键约束下按依赖顺序清空；学习/账户数据不在这些表中（第 3、4 阶段）
        string[] tables =
        [
            "lesson_exercises", "lessons", "skill_units", "courses",
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
