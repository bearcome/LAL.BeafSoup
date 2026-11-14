namespace BeafSoup.Identity;

/// <summary>注册请求：RegistrationEventId 由客户端生成，用于注册幂等（重放同一事件不重复建户、不重复计数）。</summary>
public sealed record RegisterAccountRequest(
    string Email,
    string Password,
    string? BaseLanguage,
    string? TargetLanguage,
    Guid RegistrationEventId,
    bool IsTestAccount = false);

public sealed record LoginRequest(string Email, string Password);

/// <summary>登录/注册结果：访问令牌 + 会话标识；UserId 仅来自服务端身份上下文。</summary>
public sealed record AuthTokensDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid SessionId,
    Guid UserId);

/// <summary>
/// 注册结果：启用邮箱验证要求时 Tokens 为 null，客户端转入验证流程；
/// VerificationRequired 只反映本次注册的处理方式，不泄露其他账户信息。
/// </summary>
public sealed record RegistrationResultDto(
    Guid UserId,
    string Email,
    bool VerificationRequired,
    AuthTokensDto? Tokens);

/// <summary>会话在线事实快照（供 UsageAnalytics 计算在线/时长投影；权威数据仍在 Identity 会话表）。</summary>
public sealed record SessionPresenceSnapshot(
    Guid SessionId,
    Guid UserId,
    DateTime LastHeartbeatAtUtc,
    long AccumulatedOnlineSeconds,
    bool IsEnded);

/// <summary>账户统计快照：累计注册数（非测试注册事件去重）与有效账户数。</summary>
public sealed record AccountStatisticsSnapshot(long RegisteredTotal, long ActiveAccounts);

public interface IIdentityAppService
{
    /// <summary>注册：幂等（按 RegistrationEventId 去重）；成功即创建唯一 UserId。启用验证要求时不签发令牌。</summary>
    Task<RegistrationResultDto> RegisterAsync(RegisterAccountRequest request, CancellationToken cancellationToken);

    /// <summary>登录：校验凭据与会话策略（新登录顶替旧会话，ADR-006）；启用验证要求时拒绝未验证账户。</summary>
    Task<AuthTokensDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}

public interface ISessionAppService
{
    /// <summary>记录一次服务端心跳（UTC，计时窗口约束见 ADR-007）。</summary>
    Task RecordHeartbeatAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>会话是否仍有效（在线判据 + 未结束）；JWT 校验钩子使用。</summary>
    Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>使某账户除保留会话外的全部活动会话失效（ADR-006 单会话策略）。</summary>
    Task RevokeActiveSessionsExceptAsync(Guid userId, Guid exceptSessionId, CancellationToken cancellationToken);

    /// <summary>主动结束当前会话。</summary>
    Task EndSessionAsync(Guid sessionId, CancellationToken cancellationToken);
}

/// <summary>只读统计查询：UsageAnalytics 经 Contracts 消费 Identity 的权威事实，模块间零表共享。</summary>
public interface IAccountStatisticsQuery
{
    Task<AccountStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SessionPresenceSnapshot>> GetPresenceSnapshotsAsync(CancellationToken cancellationToken);
}
