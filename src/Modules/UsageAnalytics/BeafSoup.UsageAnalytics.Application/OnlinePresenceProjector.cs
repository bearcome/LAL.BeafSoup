using BeafSoup.Identity;

namespace BeafSoup.UsageAnalytics;

/// <summary>在线投影计算：以 Identity 会话快照为权威事实，按租约策略派生在线用户/会话与累计时长。只读投影，可随时由权威事实重建。</summary>
public static class OnlinePresenceProjector
{
    public static OnlineStatisticsSnapshot Project(
        AccountStatisticsSnapshot accountStatistics,
        IReadOnlyList<SessionPresenceSnapshot> sessions,
        PresenceLeasePolicy policy,
        DateTime nowUtc)
    {
        // 过期心跳不延长租约：只有 LastHeartbeat 仍在租约内的会话才计在线
        var onlineSessions = sessions
            .Where(s => !s.IsEnded && nowUtc <= s.LastHeartbeatAtUtc.AddSeconds(policy.LeaseTtlSeconds))
            .ToArray();

        return new OnlineStatisticsSnapshot(
            accountStatistics.RegisteredTotal,
            accountStatistics.ActiveAccounts,
            onlineSessions.Select(s => s.UserId).Distinct().Count(),
            onlineSessions.Select(s => s.SessionId).Distinct().Count(),
            sessions.Sum(s => s.AccumulatedOnlineSeconds));
    }
}
