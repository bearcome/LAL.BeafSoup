namespace BeafSoup.Learning;

/// <summary>形态掌握度：以 (ProfileId, InflectionFormId) 为键单独记录各形态作答表现（词元卡的补充维度）。</summary>
public sealed class FormMastery
{
    public Guid LearnerProfileId { get; }
    public Guid InflectionFormId { get; }
    public int CorrectCount { get; private set; }
    public int IncorrectCount { get; private set; }
    public DateTime? LastSeenAtUtc { get; private set; }

    public FormMastery(Guid learnerProfileId, Guid inflectionFormId)
        => (LearnerProfileId, InflectionFormId) = (learnerProfileId, inflectionFormId);

    public void RecordOutcome(bool correct, DateTime seenAtUtc)
    {
        if (correct) CorrectCount++; else IncorrectCount++;
        LastSeenAtUtc = seenAtUtc;
    }
}
