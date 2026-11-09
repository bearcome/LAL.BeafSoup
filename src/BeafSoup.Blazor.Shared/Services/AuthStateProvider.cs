using System.Security.Claims;
using BeafSoup.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>认证状态：来自本地缓存令牌（可丢弃）；服务端以 401 为最终裁决。</summary>
public sealed class AuthStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private readonly IAuthTokenStore _tokens;

    public AuthStateProvider(IAuthTokenStore tokens) => _tokens = tokens;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var tokens = _tokens.Tokens;
        if (tokens is null || _tokens.IsExpired)
            return Task.FromResult(new AuthenticationState(Anonymous));
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, tokens.UserId.ToString()),
            new Claim("sid", tokens.SessionId.ToString()),
        ], authenticationType: "Bearer");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public void NotifyLoggedIn() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    public void NotifyLoggedOut()
    {
        _ = _tokens.ClearAsync(CancellationToken.None);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }
}
