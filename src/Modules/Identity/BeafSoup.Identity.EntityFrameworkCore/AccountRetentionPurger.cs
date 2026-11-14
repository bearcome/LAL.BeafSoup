using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Identity.EntityFrameworkCore;

/// <summary>
/// 数据保留期清理（ADR-008）：删除超过保留期的已结束会话与已关闭账户令牌。
/// 幂等；不触碰有效会话、未使用令牌与学习事实（学习事实随账户存续）。
/// </summary>
public static class AccountRetentionPurger
{
    public static async Task<(int SessionsDeleted, int TokensDeleted)> PurgeAsync(
        IdentityDbContext context, AccountRetentionPolicy policy, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var sessionCutoff = policy.SessionCutoffUtc(nowUtc);
        var tokenCutoff = policy.TokenCutoffUtc(nowUtc);

        var sessionsDeleted = await context.UserSessions
            .Where(s => s.EndedAtUtc != null && s.EndedAtUtc < sessionCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var tokensDeleted = await context.AccountTokens
            .Where(t => t.CreatedAtUtc < tokenCutoff
                && (t.ConsumedAtUtc != null || t.ExpiresAtUtc <= nowUtc || t.FailedAttempts >= AccountToken.MaxFailedAttempts))
            .ExecuteDeleteAsync(cancellationToken);

        return (sessionsDeleted, tokensDeleted);
    }
}
