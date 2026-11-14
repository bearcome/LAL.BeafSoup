using BeafSoup.SharedKernel;

namespace BeafSoup.Content;

// ---------- 内容库只读模型：对话、短文与带词元标注的句子 ----------

/// <summary>带语言与分词标注的句子读模型；Tokens 来自绑定修订号的分词，不复制全文。</summary>
public sealed record LibrarySentenceEntry(
    string SentenceId,
    SemanticKey Key,
    LanguageCode Language,
    int TextRevision,
    IReadOnlyList<SentenceToken> Tokens);

public sealed record LibraryDialogueLineEntry(
    int LineIndex,
    string SpeakerLabel,
    string SentenceId,
    VoiceRole VoiceRole,
    LanguageCode Language);

public sealed record LibraryDialogueEntry(string DialogueId, IReadOnlyList<LibraryDialogueLineEntry> Lines);

public sealed record LibraryEssayEntry(string EssayId, IReadOnlyList<string> SentenceIds);

/// <summary>内容库读模型查询（实现在 EntityFrameworkCore；只读，不写学习状态）。</summary>
public interface IContentLibraryReader
{
    Task<IReadOnlyList<LibrarySentenceEntry>> GetSentencesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LibraryDialogueEntry>> GetDialoguesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LibraryEssayEntry>> GetEssaysAsync(CancellationToken cancellationToken);
}
