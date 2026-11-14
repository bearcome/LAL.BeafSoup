using System.Collections.Concurrent;
using BeafSoup.Audio;
using BeafSoup.Content;
using BeafSoup.Localization;
using BeafSoup.SharedKernel;

namespace BeafSoup.HttpApi.Host;

/// <summary>
/// 句子音频路径查询：组合本地化模块的音色配置与音频模块的命名规则。
/// 路径格式只由 Audio 模块决定，内容模块不复制命名逻辑。
/// </summary>
public sealed class SentenceAudioAssetQuery : ISentenceAudioAssetQuery
{
    private readonly ILocalizationReader _localization;
    private readonly ConcurrentDictionary<(string Language, string Role), string?> _tokens = new();

    public SentenceAudioAssetQuery(ILocalizationReader localization) => _localization = localization;

    public async Task<string?> GetAudioPathAsync(
        string language, string sentenceId, string voiceRole, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<VoiceRole>(voiceRole, out var role))
            return null;
        var token = await GetTokenAsync(language, role, cancellationToken);
        if (token is null)
            return null;
        var languageCode = LanguageCode.Of(language);
        return AudioPathBuilder.BuildRelativePath(
            AudioAsset.Sentence(languageCode, sentenceId, role), token);
    }

    private async Task<string?> GetTokenAsync(string language, VoiceRole role, CancellationToken cancellationToken)
    {
        var key = (language, role.ToString());
        if (_tokens.TryGetValue(key, out var cached))
            return cached;
        var config = await _localization.FindVoiceRoleConfigAsync(
            LanguageCode.Of(language), role, cancellationToken);
        _tokens[key] = config?.AssetFileToken;
        return config?.AssetFileToken;
    }
}
