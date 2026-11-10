using BeafSoup.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.UsageAnalytics;

/// <summary>在线心跳与运营统计 API。心跳要求已认证；统计要求统计权限，普通用户默认拒绝。</summary>
[ApiController]
public sealed class UsageAnalyticsController(
    IPresenceHeartbeatHandler heartbeatHandler,
    IUsageStatisticsQuery statisticsQuery) : ControllerBase
{
    public const string HeartbeatPath = "/api/presence/heartbeat";
    public const string StatisticsPath = "/api/usage-analytics/statistics";

    [HttpPost(HeartbeatPath)]
    [Authorize]
    public async Task<IActionResult> RecordHeartbeat(CancellationToken cancellationToken)
    {
        await heartbeatHandler.RecordHeartbeatAsync(AuthenticatedPrincipal.RequireSessionId(User), cancellationToken);
        return NoContent();
    }

    [HttpGet(StatisticsPath)]
    [Authorize(UsageAnalyticsPermissions.StatisticsPolicy)]
    public async Task<ActionResult<OnlineStatisticsSnapshot>> GetStatistics(CancellationToken cancellationToken)
        => Ok(await statisticsQuery.GetStatisticsAsync(cancellationToken));
}
