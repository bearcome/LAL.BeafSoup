namespace BeafSoup.Identity;

/// <summary>可替换身份提供方边界（ADR-006）：新增 OAuth/OIDC = 新实现，账户与应用层零改动。</summary>
public interface IIdentityProvider
{
    string ProviderKey { get; }

    /// <summary>校验凭据；返回是否通过。实现内部负责散列比对，明文口令不出提供方。</summary>
    Task<bool> VerifyCredentialsAsync(string providerUserId, string password, CancellationToken cancellationToken);

    /// <summary>为新身份建立凭据（散列存储）。</summary>
    Task CreateCredentialsAsync(string providerUserId, string password, CancellationToken cancellationToken);
}
