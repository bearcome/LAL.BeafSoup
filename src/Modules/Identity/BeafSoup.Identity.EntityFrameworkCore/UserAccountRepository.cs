using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Identity.EntityFrameworkCore;

public sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly IdentityDbContext _context;

    public UserAccountRepository(IdentityDbContext context) => _context = context;

    public async Task<UserAccount?> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var id = userId.ToString();
        var row = await _context.UserAccounts.FindAsync([id], cancellationToken);
        return row is null ? null : await MaterializeAsync(row, cancellationToken);
    }

    public async Task<UserAccount?> FindByIdentityLinkAsync(string provider, string providerUserId, CancellationToken cancellationToken)
    {
        var link = await _context.UserIdentityLinks
            .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderUserId == providerUserId, cancellationToken);
        if (link is null)
            return null;
        var row = await _context.UserAccounts.FindAsync([link.UserId], cancellationToken);
        return row is null ? null : await MaterializeAsync(row, cancellationToken);
    }

    public async Task InsertAsync(UserAccount account, CancellationToken cancellationToken)
    {
        _context.UserAccounts.Add(IdentityPersistenceMapper.ToRow(account));
        _context.UserIdentityLinks.AddRange(account.IdentityLinks
            .Select(l => IdentityPersistenceMapper.ToRow(l, account.Id)));
    }

    public async Task UpdateAsync(UserAccount account, CancellationToken cancellationToken)
    {
        var id = account.Id.ToString();
        var row = await _context.UserAccounts.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"账户 {id} 不存在，无法更新。");
        row.Status = account.Status.ToString();
        row.EmailVerifiedAtUtc = account.EmailVerifiedAtUtc;

        // 身份关联以聚合当前集合为准增删（注销时集合为空 → 全部删除）
        var existing = await _context.UserIdentityLinks
            .Where(l => l.UserId == id)
            .ToListAsync(cancellationToken);
        var desired = account.IdentityLinks.Select(l => (l.Provider, l.ProviderUserId)).ToHashSet();
        _context.UserIdentityLinks.RemoveRange(
            existing.Where(l => !desired.Contains((l.Provider, l.ProviderUserId))));
        foreach (var link in account.IdentityLinks.Where(l =>
                     !existing.Any(e => e.Provider == l.Provider && e.ProviderUserId == l.ProviderUserId)))
        {
            _context.UserIdentityLinks.Add(IdentityPersistenceMapper.ToRow(link, account.Id));
        }
    }

    public async Task<UserRegisteredEvent?> FindRegistrationEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var row = await _context.UserRegisteredEvents.FindAsync([eventId.ToString()], cancellationToken);
        return row is null ? null : IdentityPersistenceMapper.ToDomain(row);
    }

    public Task InsertRegistrationEventAsync(UserRegisteredEvent registrationEvent, CancellationToken cancellationToken)
    {
        _context.UserRegisteredEvents.Add(IdentityPersistenceMapper.ToRow(registrationEvent));
        return Task.CompletedTask;
    }

    public Task<long> CountRegisteredEventsAsync(bool excludeTest, CancellationToken cancellationToken)
        => excludeTest
            ? _context.UserRegisteredEvents.LongCountAsync(e => !e.IsTest, cancellationToken)
            : _context.UserRegisteredEvents.LongCountAsync(cancellationToken);

    public Task<long> CountActiveAccountsAsync(bool excludeTest, CancellationToken cancellationToken)
        => _context.UserAccounts.LongCountAsync(
            a => a.Status == nameof(UserAccountStatus.Active) && (!excludeTest || !a.IsTest),
            cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);

    private async Task<UserAccount> MaterializeAsync(UserAccountRow row, CancellationToken cancellationToken)
    {
        var links = await _context.UserIdentityLinks
            .Where(l => l.UserId == row.Id)
            .ToListAsync(cancellationToken);
        return IdentityPersistenceMapper.ToDomain(row, links);
    }
}
