namespace BeafSoup.Learning;

/// <summary>复习卡：唯一键 (LearnerProfileId, ConceptId)。学习单位是概念（语义），同一概念下的词元与形态不重复建卡。</summary>
public sealed class ReviewCard : IEquatable<ReviewCard>
{
    public Guid LearnerProfileId { get; }
    public Guid ConceptId { get; }
    public FsrsCardState Fsrs { get; private set; } = new();

    public ReviewCard(Guid learnerProfileId, Guid conceptId)
        => (LearnerProfileId, ConceptId) = (learnerProfileId, conceptId);

    /// <summary>供持久化还原使用。</summary>
    public static ReviewCard Restore(Guid learnerProfileId, Guid conceptId, FsrsCardState state)
        => new(learnerProfileId, conceptId) { Fsrs = state };

    /// <summary>评级（1=Again…4=Easy）：由 FSRS 调度器更新状态；评级即复习，单纯查看/点击不算。</summary>
    public void Rate(int grade, DateTime nowUtc, FsrsParameters parameters)
        => FsrsScheduler.ApplyReview(Fsrs, grade, nowUtc, parameters);

    /// <summary>新卡入队：立即可学（到期时间=入队时刻）；仅允许新卡调用。</summary>
    public void MarkDueNow(DateTime nowUtc)
    {
        if (Fsrs.Status is not FsrsCardStatus.New)
            throw new BeafSoup.SharedKernel.DomainException(
                LearningErrorCodes.InvalidRating, "只有新卡可以入队立即可学。");
        Fsrs.DueUtc = nowUtc;
    }

    public bool Equals(ReviewCard? other)
        => other is not null && other.LearnerProfileId == LearnerProfileId && other.ConceptId == ConceptId;

    public override bool Equals(object? obj) => Equals(obj as ReviewCard);

    public override int GetHashCode() => HashCode.Combine(LearnerProfileId, ConceptId);
}
