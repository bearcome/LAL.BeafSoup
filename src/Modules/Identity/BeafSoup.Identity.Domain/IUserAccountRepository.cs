namespace BeafSoup.Identity;

/// <summary>账户仓储：UserAccount 聚合的持久化抽象（实现在 EntityFrameworkCore）。</summary>
public interface IUserAccountRepository
{
    Task<UserAccount?> FindAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserAccount?> FindByIdentityLinkAsync(string provider, string providerUserId, CancellationToken cancellationToken);
    Task InsertAsync(UserAccount account, CancellationToken cancellationToken);

    /// <summary>写回账户状态、邮箱验证时间与身份关联（关联以当前集合为准增删）。</summary>
    Task UpdateAsync(UserAccount account, CancellationToken cancellationToken);

    Task<UserRegisteredEvent?> FindRegistrationEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task InsertRegistrationEventAsync(UserRegisteredEvent registrationEvent, CancellationToken cancellationToken);
    Task<long> CountRegisteredEventsAsync(bool excludeTest, CancellationToken cancellationToken);
    Task<long> CountActiveAccountsAsync(bool excludeTest, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>账户一次性令牌仓储（验证码/重置码）：按散列查找、按用途作废、按保留期清理。</summary>
public interface IAccountTokenRepository
{
    Task<AccountToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>某账户某用途下最近一个仍有效的令牌（用于重发作废旧码）。</summary>
    Task<AccountToken?> FindOpenAsync(Guid userId, AccountTokenPurpose purpose, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>统计某账户某用途在给定时刻之后签发的令牌数（申请频率限制）。</summary>
    Task<int> CountIssuedSinceAsync(Guid userId, AccountTokenPurpose purpose, DateTime sinceUtc, CancellationToken cancellationToken);

    Task InsertAsync(AccountToken token, CancellationToken cancellationToken);
    Task UpdateAsync(AccountToken token, CancellationToken cancellationToken);

    /// <summary>删除某账户全部令牌（账户注销）；返回删除行数。</summary>
    Task<int> DeleteAllOfUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
