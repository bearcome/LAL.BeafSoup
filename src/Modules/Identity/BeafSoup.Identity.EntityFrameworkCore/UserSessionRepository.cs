using BeafSoup.Identity;
using BeafSoup.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Identity.EntityFrameworkCore;

public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly IdentityDbContext _context;

    public UserSessionRepository(IdentityDbContext context) => _context = context;

    public async Task<UserSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var row = await _context.UserSessions.FindAsync([sessionId.ToString()], cancellationToken);
        return row is null ? null : IdentityPersistenceMapper.ToDomain(row);
    }

    public Task InsertAsync(UserSession session, CancellationToken cancellationToken)
    {
        _context.UserSessions.Add(IdentityPersistenceMapper.ToRow(session));
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<UserSession>> FindActiveSessionsOfUserAsync(Guid userId, CancellationToken cancellationToken)
        => (await _context.UserSessions
                .Where(s => s.UserId == userId.ToString() && s.EndedAtUtc == null)
                .ToListAsync(cancellationToken))
            .Select(IdentityPersistenceMapper.ToDomain)
            .ToArray();

    public async Task<IReadOnlyList<UserSession>> GetAllSessionsAsync(CancellationToken cancellationToken)
        => (await _context.UserSessions.ToListAsync(cancellationToken))
            .Select(IdentityPersistenceMapper.ToDomain)
            .ToArray();

    public async Task UpdateAsync(UserSession session, CancellationToken cancellationToken)
    {
        var row = await _context.UserSessions.FindAsync([session.SessionId.ToString()], cancellationToken)
            ?? throw new DomainException(IdentityErrorCodes.SessionNotFound, $"会话 {session.SessionId} 不存在。");
        var updated = IdentityPersistenceMapper.ToRow(session);
        row.LastHeartbeatAtUtc = updated.LastHeartbeatAtUtc;
        row.AccumulatedOnlineSeconds = updated.AccumulatedOnlineSeconds;
        row.EndedAtUtc = updated.EndedAtUtc;
        row.EndReason = updated.EndReason;
    }

    public Task<int> DeleteAllOfUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userIdText = userId.ToString();
        return _context.UserSessions
            .Where(s => s.UserId == userIdText)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
