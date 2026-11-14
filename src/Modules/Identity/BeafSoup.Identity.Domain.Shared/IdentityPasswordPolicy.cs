namespace BeafSoup.Identity;

/// <summary>口令策略：注册与重置共用同一最小长度；散列算法在 IPasswordHashingService 实现中。</summary>
public static class IdentityPasswordPolicy
{
    public const int MinimumLength = 8;
}
