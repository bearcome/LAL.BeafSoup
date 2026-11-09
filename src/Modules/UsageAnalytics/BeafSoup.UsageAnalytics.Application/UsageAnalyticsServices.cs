using BeafSoup.Identity;
using Microsoft.Extensions.Options;

namespace BeafSoup.UsageAnalytics;

public sealed class UsageStatisticsQuery : IUsageStatisticsQuery
{
    private readonly IAccountStatisticsQuery _accountStatistics;
    private readonly IOptions<PresenceLeaseOptions> _options;
    private readonly TimeProvider _timeProvider;

    public UsageStatisticsQuery(
        IAccountStatisticsQuery accountStatistics,
        IOptions<PresenceLeaseOptions> options,
        TimeProvider timeProvider)
    {
        _accountStatistics = accountStatistics;
        _options = options;
        _timeProvider = timeProvider;
    }

    public async Task<OnlineStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var policy = _options.Value.ToPolicy();
        var accountStatistics = await _accountStatistics.GetStatisticsAsync(cancellationToken);
        var sessions = await _accountStatistics.GetPresenceSnapshotsAsync(cancellationToken);
        return OnlinePresenceProjector.Project(
            accountStatistics, sessions, policy, _timeProvider.GetUtcNow().UtcDateTime);
    }
}

/// <summary>心跳处理实现：委托 Identity 会话服务落库（计时窗口约束在会话服务内执行）。</summary>
public sealed class PresenceHeartbeatHandler : IPresenceHeartbeatHandler
{
    private readonly ISessionAppService _sessionAppService;

    public PresenceHeartbeatHandler(ISessionAppService sessionAppService)
        => _sessionAppService = sessionAppService;

    public Task RecordHeartbeatAsync(Guid sessionId, CancellationToken cancellationToken)
        => _sessionAppService.RecordHeartbeatAsync(sessionId, cancellationToken);
}
