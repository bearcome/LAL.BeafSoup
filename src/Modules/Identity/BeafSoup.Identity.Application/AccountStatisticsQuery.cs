namespace BeafSoup.Identity;

/// <summary>统计查询实现：把 Identity 权威事实（注册事件、账户状态、会话记录）映射为 Contracts 快照。</summary>
public sealed class AccountStatisticsQuery : IAccountStatisticsQuery
{
    private readonly IUserAccountRepository _accountRepository;
    private readonly IUserSessionRepository _sessionRepository;

    public AccountStatisticsQuery(
        IUserAccountRepository accountRepository,
        IUserSessionRepository sessionRepository)
    {
        _accountRepository = accountRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<AccountStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var registeredTotal = await _accountRepository.CountRegisteredEventsAsync(excludeTest: true, cancellationToken);
        var activeAccounts = await _accountRepository.CountActiveAccountsAsync(excludeTest: true, cancellationToken);
        return new AccountStatisticsSnapshot(registeredTotal, activeAccounts);
    }

    public async Task<IReadOnlyList<SessionPresenceSnapshot>> GetPresenceSnapshotsAsync(CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetAllSessionsAsync(cancellationToken);
        return sessions
            .Select(s => new SessionPresenceSnapshot(
                s.SessionId, s.UserId, s.LastHeartbeatAtUtc, s.AccumulatedOnlineSeconds, s.EndedAtUtc is not null))
            .ToArray();
    }
}
