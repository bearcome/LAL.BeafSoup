using BeafSoup.Identity;
using Microsoft.JSInterop;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>本地令牌缓存（可丢弃）：启动时经 RestoreAsync 从 localStorage 恢复，登录/登出写回；过期判定见 IsExpired。</summary>
public sealed class LocalAuthTokenStore : IAuthTokenStore
{
    private const string StorageKey = "beafsoup.auth-tokens";
    private readonly IJSRuntime _jsRuntime;
    private readonly TimeProvider _timeProvider;
    private AuthTokensDto? _tokens;
    private bool _restored;

    public LocalAuthTokenStore(IJSRuntime jsRuntime, TimeProvider timeProvider)
        => (_jsRuntime, _timeProvider) = (jsRuntime, timeProvider);

    public AuthTokensDto? Tokens => _tokens;

    public bool IsExpired => Tokens is null || Tokens.ExpiresAtUtc <= _timeProvider.GetUtcNow().UtcDateTime;

    public async Task RestoreAsync(CancellationToken cancellationToken)
    {
        if (_restored)
            return;
        _restored = true;
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", cancellationToken, StorageKey);
        _tokens = string.IsNullOrWhiteSpace(json)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<AuthTokensDto>(json);
    }

    public async Task StoreAsync(AuthTokensDto tokens, CancellationToken cancellationToken)
    {
        _tokens = tokens;
        _restored = true;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken,
            StorageKey, System.Text.Json.JsonSerializer.Serialize(tokens));
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        _tokens = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, StorageKey);
    }
}
