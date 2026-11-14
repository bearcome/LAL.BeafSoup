using BeafSoup.Learning;
using BeafSoup.SharedKernel;

namespace BeafSoup.Identity;

/// <summary>
/// 账户生命周期应用服务（ADR-008）：邮箱验证、密码找回、注销与数据导出。
/// 申请类接口对外一律返回成功，不泄露账户是否存在；注销要求重新验证口令。
/// </summary>
public sealed class AccountLifecycleAppService : IAccountLifecycleAppService
{
    private readonly IUserAccountRepository _accounts;
    private readonly IUserCredentialRepository _credentials;
    private readonly IUserSessionRepository _sessions;
    private readonly IAccountTokenRepository _tokens;
    private readonly IIdentityProvider _identityProvider;
    private readonly ISessionAppService _sessionService;
    private readonly SessionIssuer _sessionIssuer;
    private readonly AccountCodeService _codes;
    private readonly IAccountDataEraser _dataEraser;
    private readonly ILearnerDataExportQuery _learningExport;
    private readonly TimeProvider _timeProvider;

    public AccountLifecycleAppService(
        IUserAccountRepository accounts,
        IUserCredentialRepository credentials,
        IUserSessionRepository sessions,
        IAccountTokenRepository tokens,
        IIdentityProvider identityProvider,
        ISessionAppService sessionService,
        SessionIssuer sessionIssuer,
        AccountCodeService codes,
        IAccountDataEraser dataEraser,
        ILearnerDataExportQuery learningExport,
        TimeProvider timeProvider)
    {
        _accounts = accounts;
        _credentials = credentials;
        _sessions = sessions;
        _tokens = tokens;
        _identityProvider = identityProvider;
        _sessionService = sessionService;
        _sessionIssuer = sessionIssuer;
        _codes = codes;
        _dataEraser = dataEraser;
        _learningExport = learningExport;
        _timeProvider = timeProvider;
    }

    public async Task RequestEmailVerificationAsync(EmailRequest request, CancellationToken cancellationToken)
    {
        var email = IdentityEmail.Normalize(request.Email);
        var account = await _accounts.FindByIdentityLinkAsync(_identityProvider.ProviderKey, email, cancellationToken);
        // 不存在、已停用或已验证：一律静默返回（外部响应相同，不泄露账户状态）
        if (account is null || account.Status is not UserAccountStatus.Active || account.IsEmailVerified)
            return;
        await _codes.TrySendAsync(account, email, AccountTokenPurpose.EmailVerification, cancellationToken);
    }

    public async Task<AuthTokensDto> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var email = IdentityEmail.Normalize(request.Email);
        var account = await RequireAccountAsync(email, cancellationToken);
        await _codes.RequireValidTokenAsync(account, AccountTokenPurpose.EmailVerification, request.Code, cancellationToken);
        account.MarkEmailVerified(_timeProvider.GetUtcNow().UtcDateTime);
        await _accounts.UpdateAsync(account, cancellationToken);
        await _accounts.SaveChangesAsync(cancellationToken);
        return await _sessionIssuer.IssueAsync(account, cancellationToken);
    }

    public async Task RequestPasswordResetAsync(EmailRequest request, CancellationToken cancellationToken)
    {
        var email = IdentityEmail.Normalize(request.Email);
        var account = await _accounts.FindByIdentityLinkAsync(_identityProvider.ProviderKey, email, cancellationToken);
        if (account is null || account.Status is not UserAccountStatus.Active)
            return;
        await _codes.TrySendAsync(account, email, AccountTokenPurpose.PasswordReset, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = IdentityEmail.Normalize(request.Email);
        if (request.NewPassword.Length < IdentityPasswordPolicy.MinimumLength)
            throw new DomainException(IdentityErrorCodes.WeakPassword,
                $"口令长度不得小于 {IdentityPasswordPolicy.MinimumLength}。");
        var account = await RequireAccountAsync(email, cancellationToken);
        await _codes.RequireValidTokenAsync(account, AccountTokenPurpose.PasswordReset, request.Code, cancellationToken);

        await _identityProvider.CreateCredentialsAsync(email, request.NewPassword, cancellationToken);
        // 持有邮箱验证码即证明邮箱控制权：重置成功顺带完成邮箱验证
        account.MarkEmailVerified(_timeProvider.GetUtcNow().UtcDateTime);
        await _accounts.UpdateAsync(account, cancellationToken);
        await _accounts.SaveChangesAsync(cancellationToken);
        // 密码变更后吊销全部会话：旧令牌经会话活性校验立即失效
        await _sessionService.RevokeActiveSessionsExceptAsync(account.Id, Guid.Empty, cancellationToken);
    }

    public async Task<AccountProfileDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindAsync(userId, cancellationToken)
            ?? throw new DomainException(IdentityErrorCodes.AccountDisabled, "账户不存在或已停用。");
        account.EnsureActive();
        return new AccountProfileDto(
            account.Id, EmailOf(account), account.IsEmailVerified, account.RegisteredAtUtc);
    }

    public async Task<AccountExportDto> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await GetAccountAsync(userId, cancellationToken);
        var learning = await _learningExport.ExportAsync(userId, cancellationToken);
        return new AccountExportDto(account, _timeProvider.GetUtcNow().UtcDateTime, learning);
    }

    public async Task DeleteAccountAsync(Guid userId, DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindAsync(userId, cancellationToken)
            ?? throw new DomainException(IdentityErrorCodes.InvalidCredentials, "账户不存在或口令错误。");
        account.EnsureActive();
        var email = EmailOf(account);
        // 注销前重新验证口令：防止会话被劫持后的误删
        if (!await _identityProvider.VerifyCredentialsAsync(email, request.Password, cancellationToken))
            throw new DomainException(IdentityErrorCodes.InvalidCredentials, "口令错误，无法注销账户。");

        // 1) 学习数据（不同模块/数据库上下文）
        await _dataEraser.EraseAsync(userId, cancellationToken);

        // 2) 身份数据：令牌、会话、凭据、身份关联
        await _tokens.DeleteAllOfUserAsync(userId, cancellationToken);
        await _sessions.DeleteAllOfUserAsync(userId, cancellationToken);
        await _credentials.RemoveAsync(_identityProvider.ProviderKey, email, cancellationToken);

        // 3) 标记删除：保留匿名注册事实（UserId + 时间），邮箱等个人标识已全部移除
        account.DetachAllIdentityLinks();
        account.Delete();
        await _accounts.UpdateAsync(account, cancellationToken);
        await _accounts.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserAccount> RequireAccountAsync(string email, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByIdentityLinkAsync(_identityProvider.ProviderKey, email, cancellationToken)
            ?? throw new DomainException(IdentityErrorCodes.InvalidOrExpiredToken, "验证码无效、已过期或已使用。");
        account.EnsureActive();
        return account;
    }

    private string EmailOf(UserAccount account)
        => account.IdentityLinks
            .FirstOrDefault(l => l.Provider == _identityProvider.ProviderKey)?.ProviderUserId ?? "";
}
