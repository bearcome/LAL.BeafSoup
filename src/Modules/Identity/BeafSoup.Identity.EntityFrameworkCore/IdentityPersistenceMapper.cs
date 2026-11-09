namespace BeafSoup.Identity.EntityFrameworkCore;

/// <summary>Identity 领域对象与持久化行之间的显式映射；仓储与种子管线共用，保证读写对称。</summary>
public static class IdentityPersistenceMapper
{
    public static UserAccountRow ToRow(UserAccount account)
        => new()
        {
            Id = account.Id.ToString(),
            Status = account.Status.ToString(),
            RegisteredAtUtc = account.RegisteredAtUtc,
            IsTest = account.IsTestAccount,
        };

    public static UserAccount ToDomain(UserAccountRow row, IEnumerable<UserIdentityLinkRow> links)
        => UserAccount.Restore(
            Guid.Parse(row.Id),
            Enum.Parse<UserAccountStatus>(row.Status),
            row.RegisteredAtUtc,
            row.IsTest,
            links.Select(l => new UserIdentityLink(l.Provider, l.ProviderUserId)));

    public static UserIdentityLinkRow ToRow(UserIdentityLink link, Guid userId)
        => new() { Provider = link.Provider, ProviderUserId = link.ProviderUserId, UserId = userId.ToString() };

    public static UserCredentialRow ToRow(string provider, string providerUserId, string hash)
        => new() { Provider = provider, ProviderUserId = providerUserId, Hash = hash };

    public static UserRegisteredEventRow ToRow(UserRegisteredEvent registrationEvent)
        => new()
        {
            EventId = registrationEvent.EventId.ToString(),
            UserId = registrationEvent.UserId.ToString(),
            OccurredAtUtc = registrationEvent.OccurredAtUtc,
            IsTest = registrationEvent.IsTest,
        };

    public static UserRegisteredEvent ToDomain(UserRegisteredEventRow row)
        => new(Guid.Parse(row.EventId), Guid.Parse(row.UserId), row.OccurredAtUtc, row.IsTest);

    public static UserSessionRow ToRow(UserSession session)
        => new()
        {
            SessionId = session.SessionId.ToString(),
            UserId = session.UserId.ToString(),
            StartedAtUtc = session.StartedAtUtc,
            LastHeartbeatAtUtc = session.LastHeartbeatAtUtc,
            AccumulatedOnlineSeconds = session.AccumulatedOnlineSeconds,
            EndedAtUtc = session.EndedAtUtc,
            EndReason = session.EndReason?.ToString(),
        };

    public static UserSession ToDomain(UserSessionRow row)
        => UserSession.Restore(
            Guid.Parse(row.SessionId),
            Guid.Parse(row.UserId),
            row.StartedAtUtc,
            row.LastHeartbeatAtUtc,
            row.AccumulatedOnlineSeconds,
            row.EndedAtUtc,
            row.EndReason is null ? null : Enum.Parse<SessionEndReason>(row.EndReason));
}
