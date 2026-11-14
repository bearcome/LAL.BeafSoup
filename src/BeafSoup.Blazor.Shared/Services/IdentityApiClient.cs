using BeafSoup.Identity;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>身份 ApiClient：实现 IIdentityAppService / ISessionAppService / IAccountLifecycleAppService（Contracts 的客户端侧）。</summary>
public sealed class IdentityApiClient : ApiClientBase, IIdentityAppService, ISessionAppService, IAccountLifecycleAppService
{
    public IdentityApiClient(HttpClient http, IAuthTokenStore tokens, IConnectivityState connectivity)
        : base(http, tokens, connectivity)
    {
    }

    public Task<RegistrationResultDto> RegisterAsync(RegisterAccountRequest request, CancellationToken cancellationToken)
        => PostAsync<RegisterAccountRequest, RegistrationResultDto>("/api/identity/register", request, cancellationToken);

    public Task<AuthTokensDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        => PostAsync<LoginRequest, AuthTokensDto>("/api/identity/login", request, cancellationToken);

    public Task RecordHeartbeatAsync(Guid sessionId, CancellationToken cancellationToken)
        => PostNoContentAsync("/api/presence/heartbeat", cancellationToken);

    public Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken)
        => Task.FromResult(true); // 客户端不校验会话活性；以服务端 401 为准

    public Task RevokeActiveSessionsExceptAsync(Guid userId, Guid exceptSessionId, CancellationToken cancellationToken)
        => Task.CompletedTask; // 单会话顶替由服务端执行（ADR-006），客户端无对应端点

    public async Task EndSessionAsync(Guid sessionId, CancellationToken cancellationToken)
        => await PostNoContentAsync("/api/identity/logout", cancellationToken);

    // ---------- 账户生命周期（ADR-008） ----------

    public async Task RequestEmailVerificationAsync(EmailRequest request, CancellationToken cancellationToken)
        => await PostNoContentAsync("/api/identity/email/verification", request, cancellationToken);

    public Task<AuthTokensDto> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken)
        => PostAsync<VerifyEmailRequest, AuthTokensDto>("/api/identity/email/verify", request, cancellationToken);

    public async Task RequestPasswordResetAsync(EmailRequest request, CancellationToken cancellationToken)
        => await PostNoContentAsync("/api/identity/password/forgot", request, cancellationToken);

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
        => await PostNoContentAsync("/api/identity/password/reset", request, cancellationToken);

    public async Task<AccountProfileDto> GetAccountAsync(Guid userId, CancellationToken cancellationToken)
        => await GetAsync<AccountProfileDto>("/api/identity/account", cancellationToken)
            ?? throw new HttpRequestException("账户信息不可用。", null, System.Net.HttpStatusCode.NotFound);

    public async Task<AccountExportDto> ExportAsync(Guid userId, CancellationToken cancellationToken)
        => await GetAsync<AccountExportDto>("/api/identity/account/export", cancellationToken)
            ?? throw new HttpRequestException("数据导出不可用。", null, System.Net.HttpStatusCode.NotFound);

    public async Task DeleteAccountAsync(Guid userId, DeleteAccountRequest request, CancellationToken cancellationToken)
        => await PostNoContentAsync("/api/identity/account/delete", request, cancellationToken);
}
