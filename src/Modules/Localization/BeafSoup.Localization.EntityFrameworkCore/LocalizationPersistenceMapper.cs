using BeafSoup.SharedKernel;

namespace BeafSoup.Localization.EntityFrameworkCore;

/// <summary>LocalizedText 持久化行：同一语义键的 N 种语言版本各一行，主键 (key, language_code)。</summary>
public sealed class LocalizedTextRow
{
    public string Key { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public string Text { get; set; } = "";
    public int Revision { get; set; }
}

/// <summary>音色角色配置行：角色 → 稳定文件名片段 + Azure 语音名，主键 (language_code, role)。</summary>
public sealed class VoiceRoleConfigRow
{
    public string LanguageCode { get; set; } = "";
    public string Role { get; set; } = "";
    public string AssetFileToken { get; set; } = "";
    public string TtsVoiceName { get; set; } = "";
}

/// <summary>本地化模块领域对象与持久化行之间的显式映射；仓储与种子管线共用，保证读写对称。</summary>
public static class LocalizationPersistenceMapper
{
    public static SemanticKeyRegistration ToRow(SemanticKey key, string domain, DateTime createdAtUtc)
        => new() { Key = key.Value, Domain = domain, CreatedAtUtc = createdAtUtc };

    public static LocalizedTextRow ToRow(LocalizedText text)
        => new()
        {
            Key = text.Key.Value,
            LanguageCode = text.Language.Value,
            Text = text.Text,
            Revision = text.Revision,
        };

    public static LocalizedText ToDomain(LocalizedTextRow row)
        => LocalizedText.Restore(
            SemanticKey.Of(row.Key),
            LanguageCode.Of(row.LanguageCode),
            row.Text,
            row.Revision);

    public static VoiceRoleConfigRow ToRow(VoiceRoleConfig config)
        => new()
        {
            LanguageCode = config.Language.Value,
            Role = config.Role.ToString(),
            AssetFileToken = config.AssetFileToken,
            TtsVoiceName = config.TtsVoiceName,
        };

    public static VoiceRoleConfig ToDomain(VoiceRoleConfigRow row)
        => VoiceRoleConfig.Create(
            LanguageCode.Of(row.LanguageCode),
            Enum.Parse<VoiceRole>(row.Role),
            row.AssetFileToken,
            row.TtsVoiceName);
}
