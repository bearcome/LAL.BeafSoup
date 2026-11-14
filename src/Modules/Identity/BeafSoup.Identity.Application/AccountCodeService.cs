using BeafSoup.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeafSoup.Identity;

/// <summary>
/// 验证码/重置码的签发、频率限制与校验（ADR-008）。
/// 明文验证码只出现在邮件中；库中只保存带盐散列。发送失败不抛出，由调用方决定外部响应。
/// </summary>
public sealed class AccountCodeService
{
    public const string VerificationSubjectKey = "ui.email.verification.subject";
    public const string VerificationBodyKey = "ui.email.verification.body";
    public const string PasswordResetSubjectKey = "ui.email.password-reset.subject";
    public const string PasswordResetBodyKey = "ui.email.password-reset.body";

    private const string EmailLanguage = "zh-CN";

    private readonly IAccountTokenRepository _tokens;
    private readonly IEmailSender _emailSender;
    private readonly ILocalizedTextCatalogQuery _texts;
    private readonly IdentitySecurityOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccountCodeService> _logger;

    public AccountCodeService(
        IAccountTokenRepository tokens,
        IEmailSender emailSender,
        ILocalizedTextCatalogQuery texts,
        IOptions<IdentitySecurityOptions> options,
        TimeProvider timeProvider,
        ILogger<AccountCodeService> logger)
    {
        _tokens = tokens;
        _emailSender = emailSender;
        _texts = texts;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// 签发并发送新验证码；返回是否实际签发。
    /// 申请频率超限或邮件发送失败时返回 false（调用方对外仍返回已受理，避免泄露账户状态）。
    /// </summary>
    public async Task<bool> TrySendAsync(
        UserAccount account, string email, AccountTokenPurpose purpose, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var issuedRecently = await _tokens.CountIssuedSinceAsync(
            account.Id, purpose, now.AddHours(-1), cancellationToken);
        if (issuedRecently >= Math.Max(1, _options.MaxCodeRequestsPerHour))
        {
            _logger.LogWarning("账户 {UserId} 的 {Purpose} 申请频率超限，本次不发送。", account.Id, purpose);
            return false;
        }

        // 重新申请立即作废旧码：避免同时存在多个可用验证码
        var open = await _tokens.FindOpenAsync(account.Id, purpose, now, cancellationToken);
        if (open is not null)
        {
            open.Consume(now);
            await _tokens.UpdateAsync(open, cancellationToken);
        }

        var code = AccountTokenCrypto.GenerateCode();
        var salt = AccountTokenCrypto.GenerateSalt();
        var lifetime = LifetimeOf(purpose);
        var token = AccountToken.Issue(
            account.Id, purpose, AccountTokenCrypto.ComputeHash(salt, code), salt, now, lifetime);
        await _tokens.InsertAsync(token, cancellationToken);
        await _tokens.SaveChangesAsync(cancellationToken);

        try
        {
            await SendAsync(email, purpose, code, lifetime, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // 验证码已落库：邮件失败时用户可重新申请；不把内部错误暴露给匿名调用方
            _logger.LogError(exception, "向账户 {UserId} 发送 {Purpose} 邮件失败。", account.Id, purpose);
            return false;
        }
        return true;
    }

    /// <summary>校验验证码并在成功时标记一次性使用；失败累计尝试次数，超限自动作废。调用方负责保存账户变更。</summary>
    public async Task<AccountToken> RequireValidTokenAsync(
        UserAccount account, AccountTokenPurpose purpose, string code, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var token = await _tokens.FindOpenAsync(account.Id, purpose, now, cancellationToken)
            ?? throw new SharedKernel.DomainException(
                IdentityErrorCodes.InvalidOrExpiredToken, "验证码无效、已过期或已使用。");

        if (!AccountTokenCrypto.Matches(token.Salt, code.Trim(), token.TokenHash))
        {
            token.RegisterFailedAttempt(now);
            await _tokens.UpdateAsync(token, cancellationToken);
            await _tokens.SaveChangesAsync(cancellationToken);
            throw new SharedKernel.DomainException(
                IdentityErrorCodes.InvalidOrExpiredToken, "验证码无效、已过期或已使用。");
        }

        token.Consume(now);
        await _tokens.UpdateAsync(token, cancellationToken);
        return token;
    }

    private TimeSpan LifetimeOf(AccountTokenPurpose purpose) => TimeSpan.FromMinutes(Math.Max(1,
        purpose is AccountTokenPurpose.EmailVerification
            ? _options.VerificationCodeLifetimeMinutes
            : _options.PasswordResetCodeLifetimeMinutes));

    private async Task SendAsync(
        string email, AccountTokenPurpose purpose, string code, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var subjectKey = purpose is AccountTokenPurpose.EmailVerification ? VerificationSubjectKey : PasswordResetSubjectKey;
        var bodyKey = purpose is AccountTokenPurpose.EmailVerification ? VerificationBodyKey : PasswordResetBodyKey;
        var texts = await _texts.GetTextsAsync([subjectKey, bodyKey], EmailLanguage, cancellationToken);

        var subject = texts.GetValueOrDefault(subjectKey, "BeafSoup");
        var bodyTemplate = texts.GetValueOrDefault(bodyKey,
            "Your code is {code}. It expires in {minutes} minutes.");
        var body = bodyTemplate
            .Replace("{code}", code)
            .Replace("{minutes}", ((int)lifetime.TotalMinutes).ToString(), StringComparison.Ordinal);

        await _emailSender.SendAsync(
            new EmailMessage(email, subject, body, purpose.ToString()), cancellationToken);
    }
}
