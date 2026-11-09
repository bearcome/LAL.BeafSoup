namespace BeafSoup.Identity.EntityFrameworkCore;

public sealed class UserAccountRow
{
    public string Id { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime RegisteredAtUtc { get; set; }
    public bool IsTest { get; set; }
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
