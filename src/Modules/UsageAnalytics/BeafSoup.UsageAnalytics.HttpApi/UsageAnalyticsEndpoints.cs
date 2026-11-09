using BeafSoup.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace BeafSoup.UsageAnalytics;

/// <summary>在线心跳与运营统计端点。心跳要求已认证（sid 来自验证过的令牌）；统计端点要求统计权限（服务端默认拒绝普通用户）。</summary>
public static class UsageAnalyticsEndpoints
{
    public const string HeartbeatPath = "/api/presence/heartbeat";
    public const string StatisticsPath = "/api/usage-analytics/statistics";

    public static IEndpointRouteBuilder MapUsageAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(HeartbeatPath, async (
            HttpContext httpContext,
            IPresenceHeartbeatHandler heartbeatHandler,
            CancellationToken cancellationToken) =>
        {
            await heartbeatHandler.RecordHeartbeatAsync(AuthenticatedPrincipal.RequireSessionId(httpContext.User), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapGet(StatisticsPath, async (
            IUsageStatisticsQuery statisticsQuery,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await statisticsQuery.GetStatisticsAsync(cancellationToken);
            return Results.Ok(snapshot);
        }).RequireAuthorization(UsageAnalyticsPermissions.StatisticsPolicy);

        return app;
    }
}
