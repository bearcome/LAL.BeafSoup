using BeafSoup.SharedKernel;
namespace BeafSoup.Identity;

/// <summary>用户账户聚合根：稳定 UserId、状态与 UTC 注册时间；可挂多个提供方身份关联。凭据不进入本聚合。</summary>
public sealed class UserAccount
{
    public Guid Id { get; }
    public UserAccountStatus Status { get; private set; }
    public DateTime RegisteredAtUtc { get; }
    public bool IsTestAccount { get; }

    private readonly List<UserIdentityLink> _identityLinks = [];
    public IReadOnlyList<UserIdentityLink> IdentityLinks => _identityLinks;

    private UserAccount(Guid id, DateTime registeredAtUtc, bool isTestAccount)
        => (Id, RegisteredAtUtc, IsTestAccount) = (id, registeredAtUtc, isTestAccount);

    public static UserAccount Create(Guid userId, DateTime registeredAtUtc, bool isTestAccount = false)
        => new(userId, registeredAtUtc, isTestAccount);

    /// <summary>供持久化还原使用；调用方须保证不变量已由写入侧校验。</summary>
    public static UserAccount Restore(Guid id, UserAccountStatus status, DateTime registeredAtUtc, bool isTestAccount, IEnumerable<UserIdentityLink> links)
    {
        var account = new UserAccount(id, registeredAtUtc, isTestAccount) { Status = status };
        account._identityLinks.AddRange(links);
        return account;
    }

    public void AttachIdentityLink(UserIdentityLink link)
    {
        if (_identityLinks.Any(l => l.Provider == link.Provider && l.ProviderUserId == link.ProviderUserId))
            throw new DomainException(IdentityErrorCodes.DuplicateIdentityLink,
                $"身份关联 ({link.Provider}, {link.ProviderUserId}) 已存在。");
        _identityLinks.Add(link);
    }

    public void EnsureActive()
    {
        if (Status is not UserAccountStatus.Active)
            throw new DomainException(IdentityErrorCodes.AccountDisabled, "账户不存在或已停用。");
    }

    public void Disable() => Status = UserAccountStatus.Disabled;

    /// <summary>删除为标记删除：注册事实（累计注册数）保留，账户从有效数中消失。</summary>
    public void Delete() => Status = UserAccountStatus.Deleted;
}
