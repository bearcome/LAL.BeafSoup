namespace BeafSoup.Identity;

/// <summary>会话仓储：UserSession 的持久化抽象（实现在 EntityFrameworkCore）。</summary>
public interface IUserSessionRepository
{
    Task<UserSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken);
    Task InsertAsync(UserSession session, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserSession>> FindActiveSessionsOfUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserSession>> GetAllSessionsAsync(CancellationToken cancellationToken);
    /// <summary>整体更新可变字段（领域对象为脱管重建，变更必须显式写回行）。</summary>
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
