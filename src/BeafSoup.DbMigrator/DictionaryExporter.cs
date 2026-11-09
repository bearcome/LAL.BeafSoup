using System.Text.Encodings.Web;
using System.Text.Json;
using BeafSoup.Audio;
using BeafSoup.Localization.EntityFrameworkCore;
using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.DbMigrator;

/// <summary>概念词典 K-V 导出：从权威库读出概念/词元/范式槽位，序列化为 语义 id → 词典条目 的只读 JSON（见 docs/domain/localization-data-design.md"概念词典 K-V 缓存"）。</summary>
public static class DictionaryExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>词元音频默认音色角色（背单词卡片正面播放；对话行仍用各自角色）。</summary>
    private const VoiceRole DefaultLexemeVoiceRole = VoiceRole.Female;

    public static async Task ExportAsync(string connectionString, string outputPath)
    {
        await using var context = new VocabularyDbContext(
            MigrationRunner.CreateOptions<VocabularyDbContext>(connectionString, "__EFMigrationsHistory_Vocabulary"));
        await using var localization = new LocalizationDbContext(
            MigrationRunner.CreateOptions<LocalizationDbContext>(connectionString, "__EFMigrationsHistory_Localization"));

        // 语言 → 默认角色文件名片段（音频 URL 由服务端唯一解析，客户端不回推路径规则）
        var voiceTokens = await localization.VoiceRoleConfigs
            .Where(v => v.Role == DefaultLexemeVoiceRole.ToString())
            .ToDictionaryAsync(v => v.LanguageCode, v => v.AssetFileToken);

        var concepts = await context.LexicalConcepts.ToListAsync();
        var tree = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var concept in concepts.OrderBy(c => c.ConceptKey))
        {
            var entry = new Dictionary<string, object>
            {
                ["conceptId"] = concept.Id.ToString(),
                ["pos"] = concept.PartOfSpeech,
                ["kind"] = concept.Kind,
            };
            foreach (var group in concept.Lexemes.GroupBy(l => l.LanguageCode).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                entry[group.Key] = new Dictionary<string, object>
                {
                    ["lexemes"] = group.OrderBy(l => l.Ordinal).Select(l => ToLexemeEntry(l, voiceTokens)).ToArray(),
                };
            }
            tree[concept.ConceptKey] = entry;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(tree, JsonOptions));
    }

    private static Dictionary<string, object> ToLexemeEntry(LexemeRow lexeme, IReadOnlyDictionary<string, string> voiceTokens)
    {
        var entry = new Dictionary<string, object>
        {
            ["lemma"] = lexeme.Lemma,
            ["isPrimary"] = lexeme.IsPrimary,
            ["register"] = lexeme.Register,
            ["cefr"] = lexeme.Cefr,
        };
        // 去冗余：与 lemma 相同的展示词、空标签、空形态一律省略
        if (!string.IsNullOrEmpty(lexeme.DisplayWord))
            entry["displayWord"] = lexeme.DisplayWord;
        if (lexeme.Article is not null)
            entry["article"] = lexeme.Article;
        if (lexeme.Tags.Count > 0)
            entry["tags"] = lexeme.Tags.OrderBy(t => t.Ordinal).Select(t => t.Tag).ToArray();
        if (lexeme.Cells.Count > 0)
        {
            entry["forms"] = lexeme.Cells
                .GroupBy(c => c.ParadigmKey)
                .ToDictionary(
                    g => g.Key,
                    g => (object)g.OrderBy(c => c.SlotOrdinal).Select(c => c.Surface).ToArray(),
                    StringComparer.Ordinal);
        }
        // 单词音频 URL（预生成静态资产，命名见 audio-asset-spec.md）：短语 lemma 的 '_' 由命名规则还原
        if (voiceTokens.TryGetValue(lexeme.LanguageCode, out var token))
        {
            var article = lexeme.Article is null ? null : char.ToLowerInvariant(lexeme.Article[0]) + lexeme.Article[1..];
            var asset = AudioAsset.Word(
                LanguageCode.Of(lexeme.LanguageCode), article, lexeme.Lemma, DefaultLexemeVoiceRole);
            entry["audioUrl"] = AudioPathBuilder.BuildRelativePath(asset, token);
        }
        return entry;
    }
}
