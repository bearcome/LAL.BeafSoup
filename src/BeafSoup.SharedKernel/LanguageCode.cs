namespace BeafSoup.SharedKernel;

/// <summary>BCP-47 语言标签值对象，如 de-DE、zh-CN；统一小写规范化。</summary>
public sealed record LanguageCode
{
    public string Value { get; }

    private LanguageCode(string value) => Value = value;

    public static LanguageCode Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedKernelErrorCodes.InvalidLanguageCode, "语言代号不能为空。");
        var normalized = value.Trim().Replace('_', '-').ToLowerInvariant();
        if (!IsWellFormed(normalized))
            throw new DomainException(SharedKernelErrorCodes.InvalidLanguageCode, $"语言代号 '{value}' 不是合法的 BCP-47 标签。");
        return new LanguageCode(Canonicalize(normalized));
    }

    /// <summary>BCP-47 大小写规范化：语言小写、地区（两字母）大写、文字（四字母）首字母大写。</summary>
    private static string Canonicalize(string code)
    {
        var segments = code.Split('-');
        segments[0] = segments[0].ToLowerInvariant();
        for (var i = 1; i < segments.Length; i++)
        {
            segments[i] = segments[i] switch
            {
                { Length: 2 } s when AllLetters(s) => s.ToUpperInvariant(),
                { Length: 4 } s when AllLetters(s) => char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant(),
                var s => s.ToLowerInvariant(),
            };
        }
        return string.Join('-', segments);
    }

    private static bool IsWellFormed(string code)
    {
        var segments = code.Split('-');
        if (segments.Length > 4) return false;
        if (segments[0].Length is < 2 or > 3 || !AllLetters(segments[0])) return false;
        for (var i = 1; i < segments.Length; i++)
        {
            if (segments[i].Length is < 2 or > 8 || !AllLettersOrDigits(segments[i])) return false;
        }
        return true;
    }

    private static bool AllLetters(string s)
    {
        foreach (var c in s)
        {
            if (c is < 'a' or > 'z') return false;
        }
        return true;
    }

    private static bool AllLettersOrDigits(string s)
    {
        foreach (var c in s)
        {
            var isLetter = c is >= 'a' and <= 'z';
            var isDigit = c is >= '0' and <= '9';
            if (!isLetter && !isDigit) return false;
        }
        return true;
    }

    public static readonly LanguageCode German = Of("de-DE");
    public static readonly LanguageCode Chinese = Of("zh-CN");
    public static readonly LanguageCode English = Of("en-US");

    public override string ToString() => Value;
}
