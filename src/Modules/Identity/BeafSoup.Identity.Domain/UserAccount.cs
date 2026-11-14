using BeafSoup.SharedKernel;
namespace BeafSoup.Identity;

/// <summary>用户账户聚合根：稳定 UserId、状态、UTC 注册时间与邮箱验证时间；可挂多个提供方身份关联。凭据不进入本聚合。</summary>
public sealed class UserAccount
{
    public Guid Id { get; }
    public UserAccountStatus Status { get; private set; }
    public DateTime RegisteredAtUtc { get; }
    public bool IsTestAccount { get; }

    /// <summary>邮箱验证时间；null 表示尚未验证。</summary>
    public DateTime? EmailVerifiedAtUtc { get; private set; }

    private readonly List<UserIdentityLink> _identityLinks = [];
    public IReadOnlyList<UserIdentityLink> IdentityLinks => _identityLinks;

    private UserAccount(Guid id, DateTime registeredAtUtc, bool isTestAccount)
        => (Id, RegisteredAtUtc, IsTestAccount) = (id, registeredAtUtc, isTestAccount);

    public static UserAccount Create(Guid userId, DateTime registeredAtUtc, bool isTestAccount = false)
        => new(userId, registeredAtUtc, isTestAccount);

    /// <summary>供持久化还原使用；调用方须保证不变量已由写入侧校验。</summary>
    public static UserAccount Restore(
        Guid id, UserAccountStatus status, DateTime registeredAtUtc, bool isTestAccount,
        IEnumerable<UserIdentityLink> links, DateTime? emailVerifiedAtUtc = null)
    {
        var account = new UserAccount(id, registeredAtUtc, isTestAccount)
        {
            Status = status,
            EmailVerifiedAtUtc = emailVerifiedAtUtc,
        };
        account._identityLinks.AddRange(links);
        return account;
    }

    public bool IsEmailVerified => EmailVerifiedAtUtc is not null;

    /// <summary>标记邮箱已验证；重复验证保持首次验证时间（幂等）。</summary>
    public void MarkEmailVerified(DateTime verifiedAtUtc)
    {
        if (Status is not UserAccountStatus.Active)
            throw new DomainException(IdentityErrorCodes.AccountDisabled, "账户不存在或已停用。");
        EmailVerifiedAtUtc ??= verifiedAtUtc;
    }

    public void AttachIdentityLink(UserIdentityLink link)
    {
        if (_identityLinks.Any(l => l.Provider == link.Provider && l.ProviderUserId == link.ProviderUserId))
            throw new DomainException(IdentityErrorCodes.DuplicateIdentityLink,
                $"身份关联 ({link.Provider}, {link.ProviderUserId}) 已存在。");
        _identityLinks.Add(link);
    }

    /// <summary>注销时移除全部身份关联：邮箱等个人标识随之消失，同一邮箱可重新注册。</summary>
    public void DetachAllIdentityLinks() => _identityLinks.Clear();

    public void EnsureActive()
    {
        if (Status is not UserAccountStatus.Active)
            throw new DomainException(IdentityErrorCodes.AccountDisabled, "账户不存在或已停用。");
    }

    public void Disable() => Status = UserAccountStatus.Disabled;

    /// <summary>删除为标记删除：注册事实（累计注册数）保留，账户从有效数中消失。</summary>
    public void Delete() => Status = UserAccountStatus.Deleted;
}
