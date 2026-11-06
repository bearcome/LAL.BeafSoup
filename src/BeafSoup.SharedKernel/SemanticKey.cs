namespace BeafSoup.SharedKernel;

/// <summary>语义键：不透明稳定标识符，绝不从任何语言的文本推导，禁止改名（废弃用标记代替）。</summary>
public sealed record SemanticKey
{
    public string Value { get; }

    private SemanticKey(string value) => Value = value;

    public static SemanticKey Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedKernelErrorCodes.InvalidSemanticKey, "语义键不能为空。");
        var key = value.Trim();
        if (!IsWellFormed(key))
            throw new DomainException(SharedKernelErrorCodes.InvalidSemanticKey, $"语义键 '{value}' 格式非法（仅小写字母/数字/点/连字符，点分段非空）。");
        return new SemanticKey(key);
    }

    private static bool IsWellFormed(string key)
    {
        if (key.Length is < 3 or > 128) return false;
        foreach (var segment in key.Split('.'))
        {
            if (segment.Length == 0) return false;
            if (segment.StartsWith('-') || segment.EndsWith('-')) return false;
            foreach (var c in segment)
            {
                var ok = c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-';
                if (!ok) return false;
            }
        }
        return true;
    }

    public override string ToString() => Value;
}
