using BeafSoup.SharedKernel;
using Microsoft.Extensions.Options;

namespace BeafSoup.Identity;

/// <summary>会话应用服务：心跳、有效性校验、单会话顶替与主动结束；时间一律取服务端 UTC。</summary>
public sealed class SessionAppService : ISessionAppService
{
    private readonly IUserSessionRepository _sessionRepository;
    private readonly PresenceOptions _presence;
    private readonly TimeProvider _timeProvider;

    public SessionAppService(
        IUserSessionRepository sessionRepository,
        IOptions<PresenceOptions> presence,
        TimeProvider timeProvider)
    {
        _sessionRepository = sessionRepository;
        _presence = presence.Value;
        _timeProvider = timeProvider;
    }

    public async Task RecordHeartbeatAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await RequireSessionAsync(sessionId, cancellationToken);
        session.RecordHeartbeat(_timeProvider.GetUtcNow().UtcDateTime, _presence.MaxTimingWindowSeconds);
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        await _sessionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.FindAsync(sessionId, cancellationToken);
        if (session is null)
            return false;
        return session.IsOnline(_timeProvider.GetUtcNow().UtcDateTime, _presence.LeaseTtlSeconds);
    }

    public async Task RevokeActiveSessionsExceptAsync(Guid userId, Guid exceptSessionId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activeSessions = await _sessionRepository.FindActiveSessionsOfUserAsync(userId, cancellationToken);
        foreach (var other in activeSessions.Where(s => s.SessionId != exceptSessionId))
        {
            other.End(now, SessionEndReason.Revoked, _presence.MaxTimingWindowSeconds);
            await _sessionRepository.UpdateAsync(other, cancellationToken);
        }
        await _sessionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task EndSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await RequireSessionAsync(sessionId, cancellationToken);
        session.End(_timeProvider.GetUtcNow().UtcDateTime, SessionEndReason.Ended, _presence.MaxTimingWindowSeconds);
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        await _sessionRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserSession> RequireSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => await _sessionRepository.FindAsync(sessionId, cancellationToken)
            ?? throw new DomainException(IdentityErrorCodes.SessionNotFound, $"会话 {sessionId} 不存在。");
}
