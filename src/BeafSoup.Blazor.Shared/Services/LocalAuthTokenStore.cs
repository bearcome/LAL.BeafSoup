using BeafSoup.Identity;
using Microsoft.JSInterop;

namespace BeafSoup.Blazor.Shared.Services;

public sealed class LocalAuthTokenStore : IAuthTokenStore
{
    private const string StorageKey = "beafsoup.auth-tokens";
    private readonly IJSRuntime _jsRuntime;
    private readonly TimeProvider _timeProvider;

    public LocalAuthTokenStore(IJSRuntime jsRuntime, TimeProvider timeProvider)
        => (_jsRuntime, _timeProvider) = (jsRuntime, timeProvider);

    public AuthTokensDto? Tokens { get; private set; }
    public bool IsExpired => Tokens is null || Tokens.ExpiresAtUtc <= _timeProvider.GetUtcNow().UtcDateTime;

    public async Task StoreAsync(AuthTokensDto tokens, CancellationToken cancellationToken)
    {
        Tokens = tokens;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", cancellationToken,
            StorageKey, System.Text.Json.JsonSerializer.Serialize(tokens));
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        Tokens = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, StorageKey);
    }
}
