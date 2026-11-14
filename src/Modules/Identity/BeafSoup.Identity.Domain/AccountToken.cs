using BeafSoup.SharedKernel;

namespace BeafSoup.Identity;

/// <summary>
/// 账户一次性令牌（邮箱验证码 / 重置码）：只保存带盐散列，一次性、限时、错误尝试次数受限。
/// 明文验证码只出现在邮件中，从不进入领域对象或日志。
/// </summary>
public sealed class AccountToken
{
    /// <summary>同一令牌允许的最大错误尝试次数；达到上限自动作废。</summary>
    public const int MaxFailedAttempts = 5;

    public Guid Id { get; }
    public Guid UserId { get; }
    public AccountTokenPurpose Purpose { get; }
    public string TokenHash { get; }
    public string Salt { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime ExpiresAtUtc { get; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }

    private AccountToken(
        Guid id, Guid userId, AccountTokenPurpose purpose, string tokenHash, string salt,
        DateTime createdAtUtc, DateTime expiresAtUtc, DateTime? consumedAtUtc, int failedAttempts)
    {
        Id = id;
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        Salt = salt;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        ConsumedAtUtc = consumedAtUtc;
        FailedAttempts = failedAttempts;
    }

    public static AccountToken Issue(
        Guid userId, AccountTokenPurpose purpose, string tokenHash, string salt,
        DateTime createdAtUtc, TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException(IdentityErrorCodes.InvalidOrExpiredToken, "令牌散列不能为空。");
        if (string.IsNullOrWhiteSpace(salt))
            throw new DomainException(IdentityErrorCodes.InvalidOrExpiredToken, "令牌盐不能为空。");
        if (lifetime <= TimeSpan.Zero)
            throw new DomainException(IdentityErrorCodes.InvalidOrExpiredToken, "令牌有效期必须为正。");
        return new AccountToken(
            Guid.NewGuid(), userId, purpose, tokenHash, salt,
            createdAtUtc, createdAtUtc + lifetime, consumedAtUtc: null, failedAttempts: 0);
    }

    /// <summary>供持久化还原使用；调用方须保证不变量已由写入侧校验。</summary>
    public static AccountToken Restore(
        Guid id, Guid userId, AccountTokenPurpose purpose, string tokenHash, string salt,
        DateTime createdAtUtc, DateTime expiresAtUtc, DateTime? consumedAtUtc, int failedAttempts)
        => new(id, userId, purpose, tokenHash, salt, createdAtUtc, expiresAtUtc, consumedAtUtc, failedAttempts);

    public bool IsClosed(DateTime nowUtc)
        => ConsumedAtUtc is not null
            || nowUtc >= ExpiresAtUtc
            || FailedAttempts >= MaxFailedAttempts;

    /// <summary>记录一次错误尝试；达到上限立即作废（记为已使用），避免继续暴力尝试。</summary>
    public void RegisterFailedAttempt(DateTime nowUtc)
    {
        FailedAttempts++;
        if (FailedAttempts >= MaxFailedAttempts)
            ConsumedAtUtc ??= nowUtc;
    }

    /// <summary>成功使用：校验仍有效后标记一次性作废；无效时抛稳定错误码。</summary>
    public void Consume(DateTime nowUtc)
    {
        if (IsClosed(nowUtc))
            throw new DomainException(IdentityErrorCodes.InvalidOrExpiredToken, "验证码无效、已过期或已使用。");
        ConsumedAtUtc = nowUtc;
    }
}
