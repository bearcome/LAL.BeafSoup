using BeafSoup.Localization;
using BeafSoup.SharedKernel;

namespace BeafSoup.Content;

/// <summary>内容库查询实现：组合句子分词、本地化文本与音频路径；全部只读，不触碰学习者状态。</summary>
public sealed class ContentLibraryQuery : IContentLibraryQuery
{
    /// <summary>非对话句子的默认朗读角色（与词典单词音频一致）。</summary>
    private const string DefaultVoiceRole = "Female";

    private readonly IContentLibraryReader _reader;
    private readonly ILocalizedTextCatalogQuery _texts;
    private readonly ISentenceAudioAssetQuery _audio;

    public ContentLibraryQuery(
        IContentLibraryReader reader, ILocalizedTextCatalogQuery texts, ISentenceAudioAssetQuery audio)
        => (_reader, _texts, _audio) = (reader, texts, audio);

    public async Task<IReadOnlyList<DialogueDto>> GetDialoguesAsync(string language, CancellationToken cancellationToken)
    {
        var dialogues = await _reader.GetDialoguesAsync(cancellationToken);
        var sentences = await GetSentenceMapAsync(language, cancellationToken);
        var texts = await GetTextsAsync(sentences.Values.Select(s => s.Key), language, cancellationToken);

        var result = new List<DialogueDto>(dialogues.Count);
        foreach (var dialogue in dialogues)
        {
            var lines = new List<DialogueLineDto>(dialogue.Lines.Count);
            foreach (var line in dialogue.Lines)
            {
                var tokens = sentences.TryGetValue(line.SentenceId, out var sentence)
                    ? ToTokenDtos(sentence.Tokens)
                    : [];
                var text = sentence is not null ? texts.GetValueOrDefault(sentence.Key.Value, "") : "";
                var audio = await _audio.GetAudioPathAsync(
                    language, line.SentenceId, line.VoiceRole.ToString(), cancellationToken);
                lines.Add(new DialogueLineDto(
                    line.LineIndex, line.SpeakerLabel, line.VoiceRole.ToString(), line.SentenceId, tokens, text, audio));
            }
            if (lines.Count > 0)
                result.Add(new DialogueDto(dialogue.DialogueId, lines[0].Text, lines));
        }
        return result;
    }

    public async Task<IReadOnlyList<EssayDto>> GetEssaysAsync(string language, CancellationToken cancellationToken)
    {
        var essays = await _reader.GetEssaysAsync(cancellationToken);
        var sentences = await GetSentenceMapAsync(language, cancellationToken);
        var texts = await GetTextsAsync(sentences.Values.Select(s => s.Key), language, cancellationToken);

        var result = new List<EssayDto>(essays.Count);
        foreach (var essay in essays)
        {
            var items = new List<LibrarySentenceDto>(essay.SentenceIds.Count);
            foreach (var sentenceId in essay.SentenceIds)
            {
                if (!sentences.TryGetValue(sentenceId, out var sentence))
                    continue;
                var audio = await _audio.GetAudioPathAsync(language, sentenceId, DefaultVoiceRole, cancellationToken);
                items.Add(new LibrarySentenceDto(
                    sentenceId,
                    texts.GetValueOrDefault(sentence.Key.Value, ""),
                    sentence.TextRevision,
                    ToTokenDtos(sentence.Tokens),
                    audio));
            }
            if (items.Count > 0)
                result.Add(new EssayDto(essay.EssayId, items));
        }
        return result;
    }

    public async Task<IReadOnlyList<LibrarySentenceDto>> GetSentencesAsync(
        string language, string? conceptKey, CancellationToken cancellationToken)
    {
        var sentences = await _reader.GetSentencesAsync(cancellationToken);
        var filtered = sentences
            .Where(s => s.Language.Value == language)
            .Where(s => conceptKey is null
                || s.Tokens.Any(t => string.Equals(t.ConceptKey?.Value, conceptKey, StringComparison.Ordinal)))
            .OrderBy(s => s.SentenceId, StringComparer.Ordinal)
            .ToArray();
        var texts = await GetTextsAsync(filtered.Select(s => s.Key), language, cancellationToken);

        var result = new List<LibrarySentenceDto>(filtered.Length);
        foreach (var sentence in filtered)
        {
            var audio = await _audio.GetAudioPathAsync(language, sentence.SentenceId, DefaultVoiceRole, cancellationToken);
            result.Add(new LibrarySentenceDto(
                sentence.SentenceId,
                texts.GetValueOrDefault(sentence.Key.Value, ""),
                sentence.TextRevision,
                ToTokenDtos(sentence.Tokens),
                audio));
        }
        return result;
    }

    private async Task<Dictionary<string, LibrarySentenceEntry>> GetSentenceMapAsync(
        string language, CancellationToken cancellationToken)
        => (await _reader.GetSentencesAsync(cancellationToken))
            .Where(s => s.Language.Value == language)
            .GroupBy(s => s.SentenceId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    private Task<IReadOnlyDictionary<string, string>> GetTextsAsync(
        IEnumerable<SemanticKey> keys, string language, CancellationToken cancellationToken)
        => _texts.GetTextsAsync(keys.Select(k => k.Value).Distinct(StringComparer.Ordinal).ToArray(), language, cancellationToken);

    private static IReadOnlyList<SentenceTokenDto> ToTokenDtos(IReadOnlyList<SentenceToken> tokens)
        => tokens.Select(t => new SentenceTokenDto(t.Start, t.Length, t.Kind.ToString(), t.ConceptKey?.Value)).ToArray();
}
