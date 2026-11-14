namespace BeafSoup.Identity;

public static class IdentityErrorCodes
{
    public const string InvalidEmail = "Identity:InvalidEmail";
    public const string WeakPassword = "Identity:WeakPassword";
    public const string DuplicateIdentityLink = "Identity:DuplicateIdentityLink";
    public const string AccountDisabled = "Identity:AccountDisabled";
    public const string InvalidCredentials = "Identity:InvalidCredentials";
    public const string SessionNotFound = "Identity:SessionNotFound";
    public const string SessionEnded = "Identity:SessionEnded";
    public const string InvalidProvider = "Identity:InvalidProvider";

    /// <summary>账户邮箱尚未验证（启用验证要求时拒绝登录）。</summary>
    public const string EmailNotVerified = "Identity:EmailNotVerified";
    /// <summary>验证码/重置码无效、过期或已使用。</summary>
    public const string InvalidOrExpiredToken = "Identity:InvalidOrExpiredToken";
    /// <summary>短期申请次数超限（按邮箱 + 用途计时）。</summary>
    public const string TooManyAttempts = "Identity:TooManyAttempts";
    /// <summary>账户已被注销。</summary>
    public const string AccountDeleted = "Identity:AccountDeleted";
}
