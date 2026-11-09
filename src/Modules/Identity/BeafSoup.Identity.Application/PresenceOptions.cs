namespace BeafSoup.Identity;

/// <summary>会话与统计的服务端参数（ADR-007，值来自宿主配置）。</summary>
public sealed record PresenceOptions
{
    public long LeaseTtlSeconds { get; init; } = 90;
    public long MaxTimingWindowSeconds { get; init; } = 300;
}
