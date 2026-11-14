namespace BeafSoup.Identity;

/// <summary>账户一次性令牌用途：邮箱验证 / 密码重置。两种令牌共用同一存储与校验规则。</summary>
public enum AccountTokenPurpose { EmailVerification, PasswordReset }
