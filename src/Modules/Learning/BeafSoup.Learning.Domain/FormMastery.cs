namespace BeafSoup.Learning;

/// <summary>形态掌握度：以 (ProfileId, CellId) 为键单独记录各范式槽位作答表现（概念卡的补充维度）。</summary>
public sealed class FormMastery
{
    public Guid LearnerProfileId { get; }
    public Guid CellId { get; }
    public int CorrectCount { get; private set; }
    public int IncorrectCount { get; private set; }
    public DateTime? LastSeenAtUtc { get; private set; }

    public FormMastery(Guid learnerProfileId, Guid cellId)
        => (LearnerProfileId, CellId) = (learnerProfileId, cellId);

    public void RecordOutcome(bool correct, DateTime seenAtUtc)
    {
        if (correct) CorrectCount++; else IncorrectCount++;
        LastSeenAtUtc = seenAtUtc;
    }
}
