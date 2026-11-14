namespace BeafSoup.Content;

// ---------- 内容库只读契约：对话、短文、例句与句子音频 ----------

/// <summary>句子分词片段：零基起点/长度；概念键由发布期消歧，用于词↔句互链。</summary>
public sealed record SentenceTokenDto(int Start, int Length, string Kind, string? ConceptKey);

/// <summary>内容库句子：目标语言文本、分词标注与音频相对路径（未配置音色时为 null）。</summary>
public sealed record LibrarySentenceDto(
    string SentenceId,
    string Text,
    int TextRevision,
    IReadOnlyList<SentenceTokenDto> Tokens,
    string? AudioUrl);

public sealed record DialogueLineDto(
    int LineIndex,
    string SpeakerLabel,
    string VoiceRole,
    string SentenceId,
    IReadOnlyList<SentenceTokenDto> Tokens,
    string Text,
    string? AudioUrl);

public sealed record DialogueDto(string DialogueId, string PreviewText, IReadOnlyList<DialogueLineDto> Lines);

public sealed record EssayDto(string EssayId, IReadOnlyList<LibrarySentenceDto> Sentences);

/// <summary>内容库查询：对话/短文/例句。全部为只读内容定义，不含学习者状态。</summary>
public interface IContentLibraryQuery
{
    Task<IReadOnlyList<DialogueDto>> GetDialoguesAsync(string language, CancellationToken cancellationToken);

    Task<IReadOnlyList<EssayDto>> GetEssaysAsync(string language, CancellationToken cancellationToken);

    /// <summary>取带目标语言分词的句子；conceptKey 非空时只返回明确标注了该概念的句子。</summary>
    Task<IReadOnlyList<LibrarySentenceDto>> GetSentencesAsync(
        string language, string? conceptKey, CancellationToken cancellationToken);
}

/// <summary>句子音频路径查询：按声明的音色角色解析静态资产相对路径（实现在宿主组合层）。</summary>
public interface ISentenceAudioAssetQuery
{
    Task<string?> GetAudioPathAsync(
        string language, string sentenceId, string voiceRole, CancellationToken cancellationToken);
}
