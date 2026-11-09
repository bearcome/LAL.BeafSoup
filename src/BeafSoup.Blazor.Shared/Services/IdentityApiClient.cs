using BeafSoup.Identity;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>身份 ApiClient：实现 IIdentityAppService / ISessionAppService（Contracts 的客户端侧）。</summary>
public sealed class IdentityApiClient : ApiClientBase, IIdentityAppService, ISessionAppService
{
    public IdentityApiClient(HttpClient http, IAuthTokenStore tokens, IConnectivityState connectivity)
        : base(http, tokens, connectivity)
    {
    }

    public Task<AuthTokensDto> RegisterAsync(RegisterAccountRequest request, CancellationToken cancellationToken)
        => PostAsync<RegisterAccountRequest, AuthTokensDto>("/api/identity/register", request, cancellationToken);

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
}
