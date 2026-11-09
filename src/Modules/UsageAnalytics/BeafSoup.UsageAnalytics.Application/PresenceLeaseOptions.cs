namespace BeafSoup.UsageAnalytics;

/// <summary>宿主配置 → 租约策略 的绑定节（ADR-007 初始值）。</summary>
public sealed class PresenceLeaseOptions
{
    public const string SectionName = "Presence";

    public long HeartbeatIntervalSeconds { get; set; } = PresenceLeasePolicy.InitialDefaults.HeartbeatIntervalSeconds;
    public long LeaseTtlSeconds { get; set; } = PresenceLeasePolicy.InitialDefaults.LeaseTtlSeconds;
    public long MaxTimingWindowSeconds { get; set; } = PresenceLeasePolicy.InitialDefaults.MaxTimingWindowSeconds;

    public PresenceLeasePolicy ToPolicy()
        => new(HeartbeatIntervalSeconds, LeaseTtlSeconds, MaxTimingWindowSeconds);
}
