namespace BeafSoup.Identity;

/// <summary>账户仓储：UserAccount 聚合的持久化抽象（实现在 EntityFrameworkCore）。</summary>
public interface IUserAccountRepository
{
    Task<UserAccount?> FindAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserAccount?> FindByIdentityLinkAsync(string provider, string providerUserId, CancellationToken cancellationToken);
    Task InsertAsync(UserAccount account, CancellationToken cancellationToken);
    Task<UserRegisteredEvent?> FindRegistrationEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task InsertRegistrationEventAsync(UserRegisteredEvent registrationEvent, CancellationToken cancellationToken);
    Task<long> CountRegisteredEventsAsync(bool excludeTest, CancellationToken cancellationToken);
    Task<long> CountActiveAccountsAsync(bool excludeTest, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
