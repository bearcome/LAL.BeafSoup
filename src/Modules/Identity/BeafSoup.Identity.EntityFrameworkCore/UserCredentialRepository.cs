using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Identity.EntityFrameworkCore;

public sealed class UserCredentialRepository : IUserCredentialRepository
{
    private readonly IdentityDbContext _context;

    public UserCredentialRepository(IdentityDbContext context) => _context = context;

    public async Task<string?> FindCredentialHashAsync(string provider, string providerUserId, CancellationToken cancellationToken)
    {
        var row = await _context.UserCredentials
            .FirstOrDefaultAsync(c => c.Provider == provider && c.ProviderUserId == providerUserId, cancellationToken);
        return row?.Hash;
    }

    public async Task UpsertCredentialHashAsync(string provider, string providerUserId, string hash, CancellationToken cancellationToken)
    {
        var existing = await _context.UserCredentials
            .FirstOrDefaultAsync(c => c.Provider == provider && c.ProviderUserId == providerUserId, cancellationToken);
        if (existing is null)
            _context.UserCredentials.Add(IdentityPersistenceMapper.ToRow(provider, providerUserId, hash));
        else
            existing.Hash = hash;
    }

    public async Task RemoveAsync(string provider, string providerUserId, CancellationToken cancellationToken)
    {
        await _context.UserCredentials
            .Where(c => c.Provider == provider && c.ProviderUserId == providerUserId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
