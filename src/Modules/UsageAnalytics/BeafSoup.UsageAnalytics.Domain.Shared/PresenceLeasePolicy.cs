namespace BeafSoup.UsageAnalytics;

/// <summary>会话租约策略（ADR-007 初始值，可由宿主配置覆盖）：心跳周期仅作客户端提示，在线判据与时长口径在服务端。</summary>
public sealed record PresenceLeasePolicy(
    long HeartbeatIntervalSeconds,
    long LeaseTtlSeconds,
    long MaxTimingWindowSeconds)
{
    public static PresenceLeasePolicy InitialDefaults { get; } = new(30, 90, 300);
}
