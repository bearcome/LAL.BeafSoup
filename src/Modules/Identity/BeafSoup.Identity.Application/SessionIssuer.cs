namespace BeafSoup.Identity;

/// <summary>会话签发：注册、登录、邮箱验证共用；新会话顶替同账户旧会话（ADR-006 单会话策略）。</summary>
public sealed class SessionIssuer
{
    private readonly IUserSessionRepository _sessions;
    private readonly ISessionAppService _sessionService;
    private readonly IAccessTokenFactory _tokenFactory;
    private readonly TimeProvider _timeProvider;

    public SessionIssuer(
        IUserSessionRepository sessions,
        ISessionAppService sessionService,
        IAccessTokenFactory tokenFactory,
        TimeProvider timeProvider)
    {
        _sessions = sessions;
        _sessionService = sessionService;
        _tokenFactory = tokenFactory;
        _timeProvider = timeProvider;
    }

    public async Task<AuthTokensDto> IssueAsync(UserAccount account, CancellationToken cancellationToken)
    {
        var session = UserSession.Start(Guid.NewGuid(), account.Id, _timeProvider.GetUtcNow().UtcDateTime);
        await _sessions.InsertAsync(session, cancellationToken);
        await _sessions.SaveChangesAsync(cancellationToken);
        await _sessionService.RevokeActiveSessionsExceptAsync(account.Id, session.SessionId, cancellationToken);
        var (token, expiresAtUtc) = _tokenFactory.Create(account.Id, session.SessionId);
        return new AuthTokensDto(token, expiresAtUtc, session.SessionId, account.Id);
    }
}
