using BeafSoup.SharedKernel;
namespace BeafSoup.Identity;

/// <summary>password 提供方：经 IPasswordHashingService 完成散列比对（实现在 HttpApi 基础设施层）；散列仅存于凭据存储表。</summary>
public sealed class PasswordIdentityProvider : IIdentityProvider
{
    public const string ProviderKeyName = "password";

    private readonly IUserCredentialRepository _credentialRepository;
    private readonly IPasswordHashingService _passwordHashing;

    public PasswordIdentityProvider(
        IUserCredentialRepository credentialRepository,
        IPasswordHashingService passwordHashing)
    {
        _credentialRepository = credentialRepository;
        _passwordHashing = passwordHashing;
    }

    public string ProviderKey => ProviderKeyName;

    public async Task<bool> VerifyCredentialsAsync(string providerUserId, string password, CancellationToken cancellationToken)
    {
        var hash = await _credentialRepository.FindCredentialHashAsync(ProviderKeyName, providerUserId, cancellationToken);
        return hash is not null && _passwordHashing.Verify(providerUserId, hash, password);
    }

    public async Task CreateCredentialsAsync(string providerUserId, string password, CancellationToken cancellationToken)
    {
        var hash = _passwordHashing.Hash(providerUserId, password);
        await _credentialRepository.UpsertCredentialHashAsync(ProviderKeyName, providerUserId, hash, cancellationToken);
    }
}

/// <summary>providerUserId 规范化：邮箱统一小写，保证提供方范围内唯一。</summary>
public static class IdentityEmail
{
    public static string Normalize(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException(IdentityErrorCodes.InvalidEmail, $"邮箱 '{email}' 非法。");
        return email.Trim().ToLowerInvariant();
    }
}
