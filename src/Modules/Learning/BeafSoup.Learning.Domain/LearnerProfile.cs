using BeafSoup.SharedKernel;

namespace BeafSoup.Learning;

/// <summary>学习者配置聚合：属于一个用户；基础/目标语言、目标记忆率与主题偏好可调整；配置 ID 稳定且不可跨账户访问。</summary>
public sealed class LearnerProfile
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public LanguageCode BaseLanguage { get; }
    public LanguageCode TargetLanguage { get; }
    public double TargetRetentionRate { get; private set; }
    public string ThemePreference { get; private set; } = "system";

    private LearnerProfile(Guid id, Guid userId, LanguageCode baseLanguage, LanguageCode targetLanguage, double targetRetentionRate)
        => (Id, UserId, BaseLanguage, TargetLanguage, TargetRetentionRate) = (id, userId, baseLanguage, targetLanguage, targetRetentionRate);

    public static LearnerProfile Create(Guid id, Guid userId, LanguageCode baseLanguage, LanguageCode targetLanguage, double targetRetentionRate)
    {
        if (targetRetentionRate is <= 0 or > 1)
            throw new DomainException(LearningErrorCodes.InvalidRetentionRate, "目标记忆率必须在 (0, 1] 区间。");
        return new LearnerProfile(id, userId, baseLanguage, targetLanguage, targetRetentionRate);
    }

    public void ChangeTargetRetentionRate(double value)
    {
        if (value is <= 0 or > 1)
            throw new DomainException(LearningErrorCodes.InvalidRetentionRate, "目标记忆率必须在 (0, 1] 区间。");
        TargetRetentionRate = value;
    }

    public void ChangeTheme(string theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
            throw new DomainException(LearningErrorCodes.EmptyTheme, "主题偏好不能为空。");
        ThemePreference = theme.Trim();
    }
}
