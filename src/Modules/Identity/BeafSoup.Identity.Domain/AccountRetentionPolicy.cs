using BeafSoup.SharedKernel;

namespace BeafSoup.Identity;

/// <summary>数据保留期策略（ADR-008）：只在运维清理任务中使用；学习事实随账户存续，不在此清理范围。</summary>
public sealed record AccountRetentionPolicy(int EndedSessionRetentionDays, int AccountTokenRetentionDays)
{
    public static AccountRetentionPolicy Create(int endedSessionRetentionDays, int accountTokenRetentionDays)
    {
        if (endedSessionRetentionDays < 1)
            throw new DomainException(IdentityErrorCodes.InvalidProvider, "会话记录保留期必须不小于 1 天。");
        if (accountTokenRetentionDays < 1)
            throw new DomainException(IdentityErrorCodes.InvalidProvider, "账户令牌保留期必须不小于 1 天。");
        return new AccountRetentionPolicy(endedSessionRetentionDays, accountTokenRetentionDays);
    }

    public DateTime SessionCutoffUtc(DateTime nowUtc) => nowUtc.AddDays(-EndedSessionRetentionDays);

    public DateTime TokenCutoffUtc(DateTime nowUtc) => nowUtc.AddDays(-AccountTokenRetentionDays);
}
