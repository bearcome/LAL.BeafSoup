using BeafSoup.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Content.EntityFrameworkCore;

/// <summary>内容库读模型实现：句子分词（含概念键）、对话行与短文句子引用，全部只读。</summary>
public sealed class ContentLibraryReader : IContentLibraryReader
{
    private readonly ContentDbContext _context;

    public ContentLibraryReader(ContentDbContext context) => _context = context;

    public async Task<IReadOnlyList<LibrarySentenceEntry>> GetSentencesAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.Sentences
            .AsNoTracking()
            .Include(s => s.Tokenizations)
            .ThenInclude(t => t.Tokens)
            .ToListAsync(cancellationToken);

        return rows
            .SelectMany(sentence => sentence.Tokenizations
                // 空分词（如尚未实现中文分词）不进入内容库；同一句子可有多语言标注
                .Where(tokenization => tokenization.Tokens.Count > 0)
                .Select(tokenization => new LibrarySentenceEntry(
                    sentence.Id,
                    SemanticKey.Of(sentence.Key),
                    LanguageCode.Of(tokenization.LanguageCode),
                    tokenization.TextRevision,
                    tokenization.Tokens
                        .OrderBy(token => token.Ordinal)
                        .Select(token => new SentenceToken(
                            token.Start,
                            token.Length,
                            Enum.Parse<SentenceTokenKind>(token.Kind),
                            token.FormCellId,
                            token.ConceptKey is null ? null : SemanticKey.Of(token.ConceptKey)))
                        .ToArray())))
            .ToArray();
    }

    public async Task<IReadOnlyList<LibraryDialogueEntry>> GetDialoguesAsync(CancellationToken cancellationToken)
    {
        var dialogues = await _context.Dialogues
            .AsNoTracking()
            .Include(d => d.Lines)
            .ToListAsync(cancellationToken);

        // 对话行只存句子引用；语言取该句子拥有分词标注的语言（通常为 de-DE）
        var languages = await _context.SentenceTokens
            .AsNoTracking()
            .GroupBy(t => new { t.SentenceId, t.LanguageCode })
            .Select(g => new { g.Key.SentenceId, g.Key.LanguageCode, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var languageBySentence = languages
            .GroupBy(l => l.SentenceId)
            .ToDictionary(
                g => g.Key,
                g => LanguageCode.Of(g.OrderByDescending(l => l.Count).ThenBy(l => l.LanguageCode, StringComparer.Ordinal).First().LanguageCode));

        return dialogues.Select(dialogue => new LibraryDialogueEntry(
            dialogue.Id,
            dialogue.Lines
                .OrderBy(line => line.Ordinal)
                .Select((line, index) => new LibraryDialogueLineEntry(
                    index,
                    line.SpeakerLabel,
                    line.SentenceId,
                    Enum.Parse<VoiceRole>(line.VoiceRole),
                    languageBySentence.GetValueOrDefault(line.SentenceId, LanguageCode.Of("de-DE"))))
                .ToArray())).ToArray();
    }

    public async Task<IReadOnlyList<LibraryEssayEntry>> GetEssaysAsync(CancellationToken cancellationToken)
    {
        var essays = await _context.Essays
            .AsNoTracking()
            .Include(e => e.Items)
            .ToListAsync(cancellationToken);

        return essays.Select(essay => new LibraryEssayEntry(
            essay.Id,
            essay.Items.OrderBy(item => item.Ordinal).Select(item => item.SentenceId).ToArray())).ToArray();
    }
}
