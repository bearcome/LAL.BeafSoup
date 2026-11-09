using BeafSoup.Learning;
using BeafSoup.SharedKernel;

namespace BeafSoup.Identity;

/// <summary>身份应用服务：注册（幂等）与登录（单会话顶替）；注册成功即创建默认 LearnerProfile（验收：默认学习配置）。</summary>
public sealed class IdentityAppService : IIdentityAppService
{
    private const int MinimumPasswordLength = 8;

    private readonly IUserAccountRepository _accountRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IIdentityProvider _identityProvider;
    private readonly IAccessTokenFactory _tokenFactory;
    private readonly ISessionAppService _sessionAppService;
    private readonly ILearningProfileAppService _learningProfiles;
    private readonly TimeProvider _timeProvider;

    public IdentityAppService(
        IUserAccountRepository accountRepository,
        IUserSessionRepository sessionRepository,
        IIdentityProvider identityProvider,
        IAccessTokenFactory tokenFactory,
        ISessionAppService sessionAppService,
        ILearningProfileAppService learningProfiles,
        TimeProvider timeProvider)
    {
        _accountRepository = accountRepository;
        _sessionRepository = sessionRepository;
        _identityProvider = identityProvider;
        _tokenFactory = tokenFactory;
        _sessionAppService = sessionAppService;
        _learningProfiles = learningProfiles;
        _timeProvider = timeProvider;
    }

    public async Task<AuthTokensDto> RegisterAsync(RegisterAccountRequest request, CancellationToken cancellationToken)
    {
        var providerUserId = IdentityEmail.Normalize(request.Email);
        if (request.Password.Length < MinimumPasswordLength)
            throw new DomainException(IdentityErrorCodes.WeakPassword, $"口令长度不得小于 {MinimumPasswordLength}。");

        // 幂等：同一 RegistrationEventId 重放 → 返回既有账户的新会话，不重复建户、不重复计数
        var existingEvent = await _accountRepository.FindRegistrationEventAsync(request.RegistrationEventId, cancellationToken);
        if (existingEvent is not null)
        {
            var existingAccount = await _accountRepository.FindAsync(existingEvent.UserId, cancellationToken)
                ?? throw new DomainException(IdentityErrorCodes.InvalidCredentials, "注册事件对应的账户不存在。");
            existingAccount.EnsureActive();
            return await IssueSessionAsync(existingAccount, request, isNewAccount: false, cancellationToken);
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
        return await IssueSessionAsync(account, request, isNewAccount: true, cancellationToken);
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
        return await IssueSessionAsync(account, new RegisterAccountRequest(
            request.Email, request.Password, null, null, Guid.NewGuid()), isNewAccount: false, cancellationToken);
    }

    /// <summary>签发新会话并顶替同账户旧会话（ADR-006：每账户最多一个有效交互会话）；新账户同时创建默认学习配置。</summary>
    private async Task<AuthTokensDto> IssueSessionAsync(
        UserAccount account, RegisterAccountRequest request, bool isNewAccount, CancellationToken cancellationToken)
    {
        if (isNewAccount)
        {
            // 默认 LearnerProfile：基础/目标语言取注册请求，缺省 zh-CN → de-DE（首个里程碑）
            var baseLanguage = request.BaseLanguage ?? "zh-CN";
            var targetLanguage = request.TargetLanguage ?? "de-DE";
            await _learningProfiles.CreateAsync(account.Id,
                new CreateLearnerProfileRequest(baseLanguage, targetLanguage), cancellationToken);
        }

        var session = UserSession.Start(Guid.NewGuid(), account.Id, _timeProvider.GetUtcNow().UtcDateTime);
        await _sessionRepository.InsertAsync(session, cancellationToken);
        await _sessionRepository.SaveChangesAsync(cancellationToken);
        await _sessionAppService.RevokeActiveSessionsExceptAsync(account.Id, session.SessionId, cancellationToken);
        var (token, expiresAtUtc) = _tokenFactory.Create(account.Id, session.SessionId);
        return new AuthTokensDto(token, expiresAtUtc, session.SessionId, account.Id);
    }
}
