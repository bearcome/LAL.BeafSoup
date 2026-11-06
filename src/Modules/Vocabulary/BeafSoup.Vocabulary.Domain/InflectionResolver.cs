using BeafSoup.SharedKernel;

namespace BeafSoup.Vocabulary;

/// <summary>候选词形条目：由应用层按语言规范化后的检索数据。</summary>
public sealed record InflectionCandidate(Guid LemmaId, Guid FormId, string NormalizedSurface);

/// <summary>形态解析命中。</summary>
public sealed record InflectionMatch(Guid LemmaId, Guid FormId);

/// <summary>语言相关的表层形式规范化器；检索键按语言配置，不是全局唯一键。</summary>
public interface ISurfaceFormNormalizer
{
    LanguageCode Language { get; }
    string Normalize(string surface);
}

/// <summary>德语规范化：小写、去首尾空白；变音符号原样保留（与音频命名一致）。</summary>
public sealed class GermanSurfaceFormNormalizer : ISurfaceFormNormalizer
{
    public LanguageCode Language => LanguageCode.Of("de-DE");
    public string Normalize(string surface) => surface.Trim().ToLowerInvariant();
}

/// <summary>形态解析领域服务：规范化检索键可命中多个候选（歧义是常态），返回全部候选，绝不强制唯一归属。</summary>
public sealed class InflectionResolver
{
    public IReadOnlyList<InflectionMatch> Resolve(IReadOnlyList<InflectionCandidate> candidates, string normalizedSurface)
        => candidates
            .Where(c => c.NormalizedSurface == normalizedSurface)
            .Select(c => new InflectionMatch(c.LemmaId, c.FormId))
            .ToArray();
}
