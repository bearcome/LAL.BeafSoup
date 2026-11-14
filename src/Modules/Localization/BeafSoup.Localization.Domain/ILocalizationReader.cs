using BeafSoup.SharedKernel;
namespace BeafSoup.Localization;

/// <summary>本地化读模型查询：UI 文案与音色角色配置（实现在 EntityFrameworkCore；只读，不回写）。</summary>
public interface ILocalizationReader
{
    /// <summary>取某 domain（如 ui）下全部语义键的 (键, 语言, 文本, 修订) 行；界面文案只来自数据层。</summary>
    Task<IReadOnlyList<(SemanticKey Key, LanguageCode Language, string Text, int Revision)>> GetTextsByDomainAsync(
        string domain, CancellationToken cancellationToken);

    /// <summary>取某语言某角色的音色配置（文件名片段 + TTS 语音名）。</summary>
    Task<VoiceRoleConfig?> FindVoiceRoleConfigAsync(LanguageCode language, VoiceRole role, CancellationToken cancellationToken);

    /// <summary>按语义键批量取全部语言版本的文本（内容库按目标语言筛选；不存在返回空集合）。</summary>
    Task<IReadOnlyList<(SemanticKey Key, LanguageCode Language, string Text, int Revision)>> GetTextsByKeysAsync(
        IReadOnlyCollection<SemanticKey> keys, CancellationToken cancellationToken);
}
