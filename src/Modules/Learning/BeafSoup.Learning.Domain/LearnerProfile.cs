using BeafSoup.SharedKernel;

namespace BeafSoup.Learning;

/// <summary>学习配置聚合：属于一个用户；基础/目标语言、目标记忆率、主题与弹层偏好可调整；配置 ID 稳定且不可跨账户访问。</summary>
public sealed class LearnerProfile
{
    public static readonly IReadOnlyCollection<string> ValidInflectionDisplays = ["Common", "Full"];
    public static readonly IReadOnlyCollection<string> ValidInspectorTriggers = ["Click", "Hover"];

    public Guid Id { get; }
    public Guid UserId { get; }
    public LanguageCode BaseLanguage { get; }
    public LanguageCode TargetLanguage { get; }
    public double TargetRetentionRate { get; private set; }
    public string ThemePreference { get; private set; } = "system";
    public string InflectionDisplay { get; private set; } = "Common";
    public string InspectorTrigger { get; private set; } = "Click";

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

    /// <summary>点词弹层默认形态显示：Common（当前槽位+最高频 4 槽）| Full（全部范式槽位）。</summary>
    public void ChangeInflectionDisplay(string value)
    {
        var trimmed = (value ?? "").Trim();
        if (!ValidInflectionDisplays.Contains(trimmed))
            throw new DomainException(LearningErrorCodes.InvalidInflectionDisplay, "形态显示偏好必须是 Common 或 Full。");
        InflectionDisplay = trimmed;
    }

    /// <summary>点词弹层触发方式：Click（默认固定）| Hover（仅预览）；触屏无悬停，Hover 仅桌面可用。</summary>
    public void ChangeInspectorTrigger(string value)
    {
        var trimmed = (value ?? "").Trim();
        if (!ValidInspectorTriggers.Contains(trimmed))
            throw new DomainException(LearningErrorCodes.InvalidInspectorTrigger, "弹层触发方式必须是 Click 或 Hover。");
        InspectorTrigger = trimmed;
    }
}
