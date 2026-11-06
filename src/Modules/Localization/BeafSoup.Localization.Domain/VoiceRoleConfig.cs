using BeafSoup.SharedKernel;

namespace BeafSoup.Localization;

/// <summary>音色角色配置：角色 → 稳定文件名片段 + Azure 语音名。文件名绝不使用 Azure 语音 id。</summary>
public sealed record VoiceRoleConfig
{
    public LanguageCode Language { get; }
    public VoiceRole Role { get; }
    public string AssetFileToken { get; }
    public string TtsVoiceName { get; }

    private VoiceRoleConfig(LanguageCode language, VoiceRole role, string assetFileToken, string ttsVoiceName)
        => (Language, Role, AssetFileToken, TtsVoiceName) = (language, role, assetFileToken, ttsVoiceName);

    public static VoiceRoleConfig Create(LanguageCode language, VoiceRole role, string assetFileToken, string ttsVoiceName)
    {
        if (string.IsNullOrEmpty(assetFileToken) || assetFileToken.Length > 32 || !IsTokenWellFormed(assetFileToken))
            throw new DomainException(LocalizationErrorCodes.InvalidAssetFileToken, $"文件名片段 '{assetFileToken}' 非法（仅字母/数字/连字符，不以连字符开头结尾）。");
        if (string.IsNullOrWhiteSpace(ttsVoiceName) || ttsVoiceName.Any(char.IsWhiteSpace))
            throw new DomainException(LocalizationErrorCodes.InvalidTtsVoiceName, "Azure 语音名不能为空且不含空白。");
        return new VoiceRoleConfig(language, role, assetFileToken, ttsVoiceName);
    }

    /// <summary>校验某语言恰好配置 Male/Female/Child 三个角色，且全部同语言。</summary>
    public static IReadOnlyList<VoiceRoleConfig> EnsureCompleteSet(LanguageCode language, IEnumerable<VoiceRoleConfig> configs)
    {
        var list = configs.ToList();
        foreach (var config in list)
        {
            if (config.Language != language)
                throw new DomainException(LocalizationErrorCodes.WrongLanguage, $"音色配置语言 {config.Language.Value} 与期望语言 {language.Value} 不一致。");
        }
        foreach (var role in Enum.GetValues<VoiceRole>())
        {
            var count = list.Count(c => c.Role == role);
            if (count == 0)
                throw new DomainException(LocalizationErrorCodes.MissingVoiceRole, $"语言 {language.Value} 缺少音色角色 {role}。");
            if (count > 1)
                throw new DomainException(LocalizationErrorCodes.DuplicateVoiceRole, $"语言 {language.Value} 的音色角色 {role} 重复配置。");
        }
        return list;
    }

    private static bool IsTokenWellFormed(string token)
    {
        if (token.StartsWith('-') || token.EndsWith('-')) return false;
        foreach (var c in token)
        {
            var ok = c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-';
            if (!ok) return false;
        }
        return true;
    }
}
