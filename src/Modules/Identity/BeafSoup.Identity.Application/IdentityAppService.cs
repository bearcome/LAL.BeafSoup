using BeafSoup.Learning;
using BeafSoup.SharedKernel;
using Microsoft.Extensions.Options;

namespace BeafSoup.Identity;

/// <summary>
/// 身份应用服务：注册（幂等）与登录（单会话顶替）；注册成功即创建默认 LearnerProfile（验收：默认学习配置）。
/// 启用邮箱验证要求时注册不签发令牌，须先完成验证（ADR-008）。
/// </summary>
public sealed class IdentityAppService : IIdentityAppService
{
    private readonly IUserAccountRepository _accountRepository;
    private readonly IIdentityProvider _identityProvider;
    private readonly SessionIssuer _sessionIssuer;
    private readonly AccountCodeService _codes;
    private readonly ILearningProfileAppService _learningProfiles;
    private readonly IdentitySecurityOptions _security;
    private readonly TimeProvider _timeProvider;

    public IdentityAppService(
        IUserAccountRepository accountRepository,
        IIdentityProvider identityProvider,
        SessionIssuer sessionIssuer,
        AccountCodeService codes,
        ILearningProfileAppService learningProfiles,
        IOptions<IdentitySecurityOptions> security,
        TimeProvider timeProvider)
    {
        _accountRepository = accountRepository;
        _identityProvider = identityProvider;
        _sessionIssuer = sessionIssuer;
        _codes = codes;
        _learningProfiles = learningProfiles;
        _security = security.Value;
        _timeProvider = timeProvider;
    }

    public async Task<RegistrationResultDto> RegisterAsync(RegisterAccountRequest request, CancellationToken cancellationToken)
    {
        var providerUserId = IdentityEmail.Normalize(request.Email);
        if (request.Password.Length < IdentityPasswordPolicy.MinimumLength)
            throw new DomainException(IdentityErrorCodes.WeakPassword,
                $"口令长度不得小于 {IdentityPasswordPolicy.MinimumLength}。");

        // 幂等：同一 RegistrationEventId 重放 → 返回既有账户的处理结果，不重复建户、不重复计数
        var existingEvent = await _accountRepository.FindRegistrationEventAsync(request.RegistrationEventId, cancellationToken);
        if (existingEvent is not null)
        {
            var existingAccount = await _accountRepository.FindAsync(existingEvent.UserId, cancellationToken)
                ?? throw new DomainException(IdentityErrorCodes.InvalidCredentials, "注册事件对应的账户不存在。");
            existingAccount.EnsureActive();
            var pending = _security.RequireVerifiedEmailForLogin && !existingAccount.IsEmailVerified;
            return new RegistrationResultDto(
                existingAccount.Id, providerUserId, pending,
                pending ? null : await _sessionIssuer.IssueAsync(existingAccount, cancellationToken));
        }

        var existing = await _accountRepository.FindByIdentityLinkAsync(_identityProvider.ProviderKey, providerUserId, cancellationToken);
        if (existing is not null)
            throw new DomainException(IdentityErrorCodes.DuplicateIdentityLink, "该邮箱已注册。");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var userId = Guid.NewGuid();
        var account = UserAccount.Create(userId, now, request.IsTestAccount);
        account.AttachIdentityLink(new UserIdentityLink(_identityProvider.ProviderKey, providerUserId));
        await _identityProvider.CreateCredentialsAsync(providerUserId, request.Password, cancellationToken);
        await _accountRepository.InsertAsync(account, cancellationToken);
        await _accountRepository.InsertRegistrationEventAsync(
            new UserRegisteredEvent(request.RegistrationEventId, userId, now, request.IsTestAccount), cancellationToken);
        await _accountRepository.SaveChangesAsync(cancellationToken);

        // 默认 LearnerProfile：基础/目标语言取注册请求，缺省 zh-CN → de-DE（首个里程碑）
        await _learningProfiles.CreateAsync(account.Id, new CreateLearnerProfileRequest(
            request.BaseLanguage ?? "zh-CN", request.TargetLanguage ?? "de-DE"), cancellationToken);

        if (!_security.RequireVerifiedEmailForLogin)
            return new RegistrationResultDto(account.Id, providerUserId, false,
                await _sessionIssuer.IssueAsync(account, cancellationToken));

        // 验证要求：发送验证码，不签发令牌；发送失败不影响账户创建（可重新申请）
        await _codes.TrySendAsync(account, providerUserId, AccountTokenPurpose.EmailVerification, cancellationToken);
        return new RegistrationResultDto(account.Id, providerUserId, true, null);
    }

    public async Task<AuthTokensDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var providerUserId = IdentityEmail.Normalize(request.Email);
        var account = await _accountRepository.FindByIdentityLinkAsync(_identityProvider.ProviderKey, providerUserId, cancellationToken);
        // 账户不存在也执行一次散列校验，避免时间侧信道泄露账户是否存在
        var verified = await _identityProvider.VerifyCredentialsAsync(providerUserId, request.Password, cancellationToken);
        if (account is null || !verified)
            throw new DomainException(IdentityErrorCodes.InvalidCredentials, "邮箱或口令错误。");
        account.EnsureActive();
        if (_security.RequireVerifiedEmailForLogin && !account.IsEmailVerified)
            throw new DomainException(IdentityErrorCodes.EmailNotVerified, "邮箱尚未验证，请先完成验证。");
        return await _sessionIssuer.IssueAsync(account, cancellationToken);
    }
}
