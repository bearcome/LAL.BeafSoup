using BeafSoup.Identity;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>认证令牌本地存储：浏览器存储仅为可丢弃缓存（ADR-004），权威会话状态在服务端。</summary>
public interface IAuthTokenStore
{
    AuthTokensDto? Tokens { get; }
    bool IsExpired { get; }
    Task StoreAsync(AuthTokensDto tokens, CancellationToken cancellationToken);
    Task ClearAsync(CancellationToken cancellationToken);
}
