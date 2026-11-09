namespace BeafSoup.UsageAnalytics;

/// <summary>在线统计快照：在线用户按不同 UserId 去重；在线会话数按不同 SessionId 计；时长为心跳覆盖估算（不表示专注时长）。</summary>
public sealed record OnlineStatisticsSnapshot(
    long RegisteredTotal,
    long ActiveAccounts,
    long OnlineUsers,
    long OnlineSessions,
    long AccumulatedOnlineSeconds);
