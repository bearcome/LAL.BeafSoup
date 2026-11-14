namespace BeafSoup.Identity;

/// <summary>身份安全配置（ADR-008）：登录是否要求已验证邮箱、验证码有效期与申请频率。</summary>
public sealed class IdentitySecurityOptions
{
    public const string SectionName = "Identity";

    /// <summary>为 true 时注册不签发令牌，未验证账户登录被拒绝（默认开启）。</summary>
    public bool RequireVerifiedEmailForLogin { get; init; } = true;

    public int VerificationCodeLifetimeMinutes { get; init; } = 30;

    public int PasswordResetCodeLifetimeMinutes { get; init; } = 30;

    /// <summary>同一账户同一用途每小时最多签发次数；超出后静默跳过发送。</summary>
    public int MaxCodeRequestsPerHour { get; init; } = 5;
}
