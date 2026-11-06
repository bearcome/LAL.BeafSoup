using BeafSoup.SharedKernel;

namespace BeafSoup.Audio;

/// <summary>按 audio-asset-spec.md 的命名规范把资产身份解析为不可变相对路径；文件改名/改约定 = 破坏性变更，须同步更新规范。</summary>
public static class AudioPathBuilder
{
    public const string FileExtension = ".mp3";
    private const string TokenAllowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

    public static string BuildRelativePath(AudioAsset asset, string assetFileToken)
    {
        ValidateToken(assetFileToken);
        var name = asset.Type switch
        {
            AudioAssetType.Word => WordFileName(asset, assetFileToken),
            AudioAssetType.Sentence => $"{asset.SentenceId}-{assetFileToken}",
            _ => throw new ArgumentOutOfRangeException(nameof(asset)),
        };
        return $"{asset.Language.Value}/{name}{FileExtension}";
    }

    private static string WordFileName(AudioAsset asset, string token)
    {
        // 词内空格转连字符；变音符号（ä/ö/ü/ß）原样保留
        var lemma = asset.LemmaText!.Replace(' ', '-');
        return asset.Article is null ? $"{lemma}-{token}" : $"{asset.Article}-{lemma}-{token}";
    }

    private static void ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 32 || token.StartsWith('-') || token.EndsWith('-'))
            throw new DomainException(AudioErrorCodes.InvalidAssetFileToken, $"文件名片段 '{token}' 非法。");
        if (token.Any(c => !TokenAllowed.Contains(c)))
            throw new DomainException(AudioErrorCodes.InvalidAssetFileToken, $"文件名片段 '{token}' 含非法字符。");
    }
}
