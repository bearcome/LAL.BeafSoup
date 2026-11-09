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
