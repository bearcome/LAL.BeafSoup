using BeafSoup.SharedKernel;

namespace BeafSoup.Content;

/// <summary>短文：有序句子列表；音频由句子资产前端拼接，无独立 TTS 文件。</summary>
public sealed class Essay
{
    public string Id { get; }
    private readonly List<string> _sentenceIds = [];
    public IReadOnlyList<string> SentenceIds => _sentenceIds;

    public Essay(string id) => Id = SlugId.Validate(id, nameof(id));

    public void AppendSentence(string sentenceId)
    {
        var valid = SlugId.Validate(sentenceId, nameof(sentenceId));
        if (_sentenceIds.Contains(valid))
            throw new DomainException(ContentErrorCodes.DuplicateSentence, $"短文 {Id} 已包含句子 {valid}。");
        _sentenceIds.Add(valid);
    }
}
