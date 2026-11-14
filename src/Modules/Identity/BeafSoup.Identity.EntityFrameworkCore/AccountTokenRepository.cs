using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Identity.EntityFrameworkCore;

/// <summary>账户令牌仓储：一次性验证码/重置码的持久化（ADR-008）。</summary>
public sealed class AccountTokenRepository : IAccountTokenRepository
{
    private readonly IdentityDbContext _context;

    public AccountTokenRepository(IdentityDbContext context) => _context = context;

    public async Task<AccountToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var row = await _context.AccountTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        return row is null ? null : IdentityPersistenceMapper.ToDomain(row);
    }

    public async Task<AccountToken?> FindOpenAsync(
        Guid userId, AccountTokenPurpose purpose, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var userIdText = userId.ToString();
        var purposeText = purpose.ToString();
        var row = await _context.AccountTokens
            .Where(t => t.UserId == userIdText && t.Purpose == purposeText)
            .Where(t => t.ConsumedAtUtc == null && t.ExpiresAtUtc > nowUtc && t.FailedAttempts < AccountToken.MaxFailedAttempts)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : IdentityPersistenceMapper.ToDomain(row);
    }

    public Task<int> CountIssuedSinceAsync(
        Guid userId, AccountTokenPurpose purpose, DateTime sinceUtc, CancellationToken cancellationToken)
    {
        var userIdText = userId.ToString();
        var purposeText = purpose.ToString();
        return _context.AccountTokens.CountAsync(
            t => t.UserId == userIdText && t.Purpose == purposeText && t.CreatedAtUtc >= sinceUtc,
            cancellationToken);
    }

    public Task InsertAsync(AccountToken token, CancellationToken cancellationToken)
    {
        _context.AccountTokens.Add(IdentityPersistenceMapper.ToRow(token));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(AccountToken token, CancellationToken cancellationToken)
    {
        var row = await _context.AccountTokens.FindAsync([token.Id.ToString()], cancellationToken)
            ?? throw new InvalidOperationException($"账户令牌 {token.Id} 不存在，无法更新。");
        IdentityPersistenceMapper.Apply(row, token);
    }

    public async Task<int> DeleteAllOfUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userIdText = userId.ToString();
        return await _context.AccountTokens
            .Where(t => t.UserId == userIdText)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
