using System.Globalization;
using System.Text;

namespace BeafSoup.SharedKernel;

/// <summary>判分/匹配用的冠词容忍策略：严格 = 答案必须含声明的冠词形式；容忍 = 可省略前导冠词。</summary>
public enum ArticleTolerance
{
    Strict,
    TolerateLeadingArticle,
}

/// <summary>按语言配置的文本规范化器（localization-data-design.md 的"已验证文本规范化"能力）：用于输入判分与表层形式匹配。</summary>
public interface ITextNormalizer
{
    LanguageCode Language { get; }

    /// <summary>规范化输入：去首尾空白、折叠内部空白、按语言规则折叠大小写/变音符号/标点，可按策略容忍前导冠词。</summary>
    string Normalize(string input, ArticleTolerance articleTolerance);
}

/// <summary>语言规范化器注册表：仅返回已验证的处理器；未覆盖的语言明确抛错，绝不静默套用通用规则。</summary>
public static class TextNormalizerRegistry
{
    private static readonly IReadOnlyDictionary<string, ITextNormalizer> Normalizers =
        new Dictionary<string, ITextNormalizer>(StringComparer.OrdinalIgnoreCase)
        {
            ["de-DE"] = new GermanTextNormalizer(),
            ["zh-CN"] = new ChineseTextNormalizer(),
        };

    public static ITextNormalizer For(LanguageCode language)
        => Normalizers.TryGetValue(language.Value, out var normalizer)
            ? normalizer
            : throw new DomainException(
                SharedKernelErrorCodes.UnsupportedLanguageProcessing,
                $"语言 '{language.Value}' 尚无已验证的文本规范化处理器。");
}

/// <summary>德语判分规范化：小写折叠、变音符号折叠（ä→a、ö→o、ü→u、ß→ss）、去标点、折叠空白；可按策略去掉前导冠词。</summary>
public sealed class GermanTextNormalizer : ITextNormalizer
{
    private static readonly HashSet<string> LeadingArticles = new(StringComparer.Ordinal)
    {
        "der", "die", "das", "den", "dem", "des", "ein", "eine", "einen", "einem", "einer", "eines",
    };

    public LanguageCode Language => LanguageCode.German;

    public string Normalize(string input, ArticleTolerance articleTolerance)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";
        var folded = FoldDiacritics(input.Trim().ToLowerInvariant());
        // 去标点（保留字母/数字/空白），再折叠空白
        var builder = new StringBuilder(folded.Length);
        foreach (var c in folded)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is UnicodeCategory.LowercaseLetter or UnicodeCategory.UppercaseLetter
                or UnicodeCategory.OtherLetter or UnicodeCategory.DecimalDigitNumber
                or UnicodeCategory.SpaceSeparator or UnicodeCategory.ConnectorPunctuation)
                builder.Append(c);
        }
        var normalized = CollapseWhitespace(builder.ToString());
        if (articleTolerance is ArticleTolerance.TolerateLeadingArticle)
        {
            var firstSpace = normalized.IndexOf(' ');
            if (firstSpace > 0 && LeadingArticles.Contains(normalized[..firstSpace]))
                normalized = normalized[(firstSpace + 1)..];
        }
        return normalized;
    }

    /// <summary>变音符号折叠：先显式替换 ß/ẞ，再按 NFD 分解去除组合音符（ä=a+¨ → a）。</summary>
    private static string FoldDiacritics(string input)
    {
        var builder = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (c is 'ß' or 'ẞ') { builder.Append("ss"); continue; }
            builder.Append(c);
        }
        var decomposed = builder.ToString().Normalize(NormalizationForm.FormD);
        builder.Clear();
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) is not UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString();
    }

    private static string CollapseWhitespace(string input)
    {
        var builder = new StringBuilder(input.Length);
        var pending = false;
        foreach (var c in input)
        {
            if (c is ' ' or '\t' or '\n' or '\r') { pending = builder.Length > 0; continue; }
            if (pending) { builder.Append(' '); pending = false; }
            builder.Append(c);
        }
        return builder.ToString();
    }
}

/// <summary>中文判分规范化：无大小写与变音概念，仅去首尾空白并折叠内部空白（保留汉字与内嵌拉丁字符）。</summary>
public sealed class ChineseTextNormalizer : ITextNormalizer
{
    public LanguageCode Language => LanguageCode.Chinese;

    public string Normalize(string input, ArticleTolerance articleTolerance)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";
        var parts = input.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', parts);
    }
}
