namespace BeafSoup.Identity;

/// <summary>访问令牌工厂：签发携带 sub/sid/jti 的 JWT（ADR-006）。</summary>
public interface IAccessTokenFactory
{
    (string Token, DateTime ExpiresAtUtc) Create(Guid userId, Guid sessionId);
}
