namespace BeafSoup.Learning;

/// <summary>复习卡：唯一键 (LearnerProfileId, LemmaId)。学习单位是词元，不为每个屈折形式重复建卡。</summary>
public sealed class ReviewCard : IEquatable<ReviewCard>
{
    public Guid LearnerProfileId { get; }
    public Guid LemmaId { get; }
    public FsrsCardState Fsrs { get; } = new();

    public ReviewCard(Guid learnerProfileId, Guid lemmaId)
        => (LearnerProfileId, LemmaId) = (learnerProfileId, lemmaId);

    public bool Equals(ReviewCard? other)
        => other is not null && other.LearnerProfileId == LearnerProfileId && other.LemmaId == LemmaId;

    public override bool Equals(object? obj) => Equals(obj as ReviewCard);

    public override int GetHashCode() => HashCode.Combine(LearnerProfileId, LemmaId);
}
