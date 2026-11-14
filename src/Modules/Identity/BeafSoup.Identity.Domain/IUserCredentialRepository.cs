namespace BeafSoup.Identity;

/// <summary>凭据存储抽象：由身份提供方实现语义（password 提供方存 PBKDF2 散列）；领域层只见散列串，不见明文口令。</summary>
public interface IUserCredentialRepository
{
    Task<string?> FindCredentialHashAsync(string provider, string providerUserId, CancellationToken cancellationToken);
    Task UpsertCredentialHashAsync(string provider, string providerUserId, string hash, CancellationToken cancellationToken);

    /// <summary>删除某提供方身份的凭据（账户注销）；不存在时静默返回。</summary>
    Task RemoveAsync(string provider, string providerUserId, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
