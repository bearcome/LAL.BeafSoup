namespace BeafSoup.UsageAnalytics;

/// <summary>使用统计查询：运营统计只向有服务端统计权限的操作者开放（服务端默认拒绝普通用户）。</summary>
public interface IUsageStatisticsQuery
{
    Task<OnlineStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken);
}

/// <summary>心跳处理：接受心跳并更新会话在线事实（组合 Identity 的会话服务）。</summary>
public interface IPresenceHeartbeatHandler
{
    Task RecordHeartbeatAsync(Guid sessionId, CancellationToken cancellationToken);
}

/// <summary>稳定权限标识（账户规范：权限按稳定标识建模，管理员/统计权限默认拒绝，不隐含授予注册用户）。</summary>
public static class UsageAnalyticsPermissions
{
    public const string StatisticsPolicy = "usage-analytics:statistics";
}
