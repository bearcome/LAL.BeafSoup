using BeafSoup.SharedKernel;

namespace BeafSoup.Content;

/// <summary>内容实体稳定 id（英文 slug 或数字，如 s-000001）：小写字母/数字/连字符，不以连字符开头结尾。</summary>
internal static class SlugId
{
    public static string Validate(string id, string paramName)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64)
            throw new DomainException(ContentErrorCodes.InvalidContentId, $"内容 id '{id}' 非法。");
        if (id.StartsWith('-') || id.EndsWith('-'))
            throw new DomainException(ContentErrorCodes.InvalidContentId, $"内容 id '{id}' 非法（不能以连字符开头或结尾）。");
        foreach (var c in id)
        {
            var ok = c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-';
            if (!ok)
                throw new DomainException(ContentErrorCodes.InvalidContentId, $"内容 id '{id}' 含非法字符。");
        }
        return id;
    }
}
