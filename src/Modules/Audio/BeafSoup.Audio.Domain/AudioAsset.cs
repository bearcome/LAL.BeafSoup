using BeafSoup.SharedKernel;

namespace BeafSoup.Audio;

/// <summary>音频资产身份：由内容 ID、语言、资产类型及音色角色解析为不可变路径；不存可变 URL。</summary>
public sealed record AudioAsset
{
    public AudioAssetType Type { get; }
    public LanguageCode Language { get; }
    public VoiceRole Role { get; }
    public string? Article { get; }
    public string? LemmaText { get; }
    public string? SentenceId { get; }

    private AudioAsset(AudioAssetType type, LanguageCode language, VoiceRole role, string? article, string? lemmaText, string? sentenceId)
        => (Type, Language, Role, Article, LemmaText, SentenceId) = (type, language, role, article, lemmaText, sentenceId);

    public static AudioAsset Word(LanguageCode language, string? article, string lemmaText, VoiceRole role)
    {
        if (article is not null && article is not ("der" or "die" or "das"))
            throw new DomainException(AudioErrorCodes.InvalidArticle, $"名词冠词必须为小写 der/die/das，实际为 '{article}'。");
        if (string.IsNullOrWhiteSpace(lemmaText))
            throw new DomainException(AudioErrorCodes.InvalidLemmaText, "词元文本不能为空。");
        return new AudioAsset(AudioAssetType.Word, language, role, article, lemmaText.Trim(), sentenceId: null);
    }

    public static AudioAsset Sentence(LanguageCode language, string sentenceId, VoiceRole role)
    {
        ValidateSentenceId(sentenceId);
        return new AudioAsset(AudioAssetType.Sentence, language, role, article: null, lemmaText: null, sentenceId);
    }

    private static void ValidateSentenceId(string sentenceId)
    {
        if (string.IsNullOrWhiteSpace(sentenceId) || sentenceId.Length > 64)
            throw new DomainException(AudioErrorCodes.InvalidSentenceId, $"句子 id '{sentenceId}' 非法。");
        if (sentenceId.StartsWith('-') || sentenceId.EndsWith('-'))
            throw new DomainException(AudioErrorCodes.InvalidSentenceId, $"句子 id '{sentenceId}' 非法（不能以连字符开头或结尾）。");
        foreach (var c in sentenceId)
        {
            var ok = c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-';
            if (!ok)
                throw new DomainException(AudioErrorCodes.InvalidSentenceId, $"句子 id '{sentenceId}' 含非法字符（语言由目录区分，id 不含语言前缀）。");
        }
    }
}
