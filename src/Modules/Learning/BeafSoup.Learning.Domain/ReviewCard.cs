namespace BeafSoup.Learning;

/// <summary>复习卡：唯一键 (LearnerProfileId, ConceptId)。学习单位是概念（语义），同一概念下的词元与形态不重复建卡。</summary>
public sealed class ReviewCard : IEquatable<ReviewCard>
{
    public Guid LearnerProfileId { get; }
    public Guid ConceptId { get; }
    public FsrsCardState Fsrs { get; } = new();

    public ReviewCard(Guid learnerProfileId, Guid conceptId)
        => (LearnerProfileId, ConceptId) = (learnerProfileId, conceptId);

    public bool Equals(ReviewCard? other)
        => other is not null && other.LearnerProfileId == LearnerProfileId && other.ConceptId == ConceptId;

    public override bool Equals(object? obj) => Equals(obj as ReviewCard);

    public override int GetHashCode() => HashCode.Combine(LearnerProfileId, ConceptId);
}
