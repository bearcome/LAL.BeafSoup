using BeafSoup.SharedKernel;
namespace BeafSoup.Identity;

/// <summary>身份关联：登录身份在所属提供方范围内唯一，并映射到稳定 UserId；凭据由提供方管理，不进入领域对象。</summary>
public sealed record UserIdentityLink
{
    public string Provider { get; }
    public string ProviderUserId { get; }

    public UserIdentityLink(string provider, string providerUserId)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new DomainException(IdentityErrorCodes.InvalidProvider, "身份提供方键不能为空。");
        if (string.IsNullOrWhiteSpace(providerUserId))
            throw new DomainException(IdentityErrorCodes.InvalidEmail, "提供方内身份标识不能为空。");
        Provider = provider.Trim();
        ProviderUserId = providerUserId.Trim();
    }
}
