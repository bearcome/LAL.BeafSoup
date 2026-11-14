namespace BeafSoup.Identity.EntityFrameworkCore;

public sealed class UserAccountRow
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime RegisteredAtUtc { get; set; }
    public bool IsTest { get; set; }

    /// <summary>邮箱验证时间；null 表示未验证（ADR-008）。</summary>
    public DateTime? EmailVerifiedAtUtc { get; set; }
}

/// <summary>账户一次性令牌行：只存带盐散列，明文验证码不落库。</summary>
public sealed class AccountTokenRow
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string Salt { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public int FailedAttempts { get; set; }
}

public sealed class UserIdentityLinkRow
{
    public string Provider { get; set; } = "";
    public string ProviderUserId { get; set; } = "";
    public string UserId { get; set; } = "";
}

public sealed class UserCredentialRow
{
    public string Provider { get; set; } = "";
    public string ProviderUserId { get; set; } = "";
    public string Hash { get; set; } = "";
}

public sealed class UserRegisteredEventRow
{
    public string EventId { get; set; } = "";
    public string UserId { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
    public bool IsTest { get; set; }
}

public sealed class UserSessionRow
{
    public string SessionId { get; set; } = "";
    public string UserId { get; set; } = "";
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastHeartbeatAtUtc { get; set; }
    public long AccumulatedOnlineSeconds { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string? EndReason { get; set; }
}
