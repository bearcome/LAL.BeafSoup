namespace BeafSoup.Identity;

// ---------- 邮件发送抽象（ADR-008：宿主负责提供实现） ----------

/// <summary>待发送邮件；正文只有纯文本与本项目生成的链接，不含任何凭据散列。</summary>
public sealed record EmailMessage(string To, string Subject, string TextBody, string Kind);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

// ---------- 账户生命周期请求/响应 ----------

public sealed record EmailRequest(string Email);

/// <summary>验证码校验请求（邮箱 + 6 位验证码）。</summary>
public sealed record VerifyEmailRequest(string Email, string Code);

public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);

/// <summary>注销账户：必须重新输入当前密码确认。</summary>
public sealed record DeleteAccountRequest(string Password);

/// <summary>当前账户基本信息（设置页展示；不含凭据与令牌）。</summary>
public sealed record AccountProfileDto(Guid UserId, string Email, bool EmailVerified, DateTime RegisteredAtUtc);

/// <summary>本人数据导出：账户信息 + 学习数据。</summary>
public sealed record AccountExportDto(
    AccountProfileDto Account,
    DateTime ExportedAtUtc,
    BeafSoup.Learning.LearnerDataExportDto Learning);

public interface IAccountLifecycleAppService
{
    /// <summary>申请邮箱验证码：无论账户是否存在都返回成功（不泄露账户存在性）。</summary>
    Task RequestEmailVerificationAsync(EmailRequest request, CancellationToken cancellationToken);

    /// <summary>校验验证码：成功后标记邮箱已验证并签发会话。</summary>
    Task<AuthTokensDto> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken);

    /// <summary>申请密码重置码：无论账户是否存在都返回成功（不泄露账户存在性）。</summary>
    Task RequestPasswordResetAsync(EmailRequest request, CancellationToken cancellationToken);

    /// <summary>用重置码设置新密码：吊销全部会话并标记邮箱已验证。</summary>
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    Task<AccountProfileDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken);

    Task<AccountExportDto> ExportAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>注销：删除个人数据并标记账户已删除（ADR-008）。</summary>
    Task DeleteAccountAsync(Guid userId, DeleteAccountRequest request, CancellationToken cancellationToken);
}
