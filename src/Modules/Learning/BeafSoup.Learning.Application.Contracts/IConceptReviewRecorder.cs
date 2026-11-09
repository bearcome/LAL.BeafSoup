namespace BeafSoup.Learning;

/// <summary>概念复习结果记录：由判分等用例调用——更新概念 FSRS 卡（按映射评级）与适用的形态掌握度；不产生活动（活动归属调用方）。</summary>
public interface IConceptReviewRecorder
{
    /// <summary>记录一次明确关联概念/词形的判分结果。评级映射：对=Good(3)，错=Again(1)。</summary>
    Task RecordGradedOutcomeAsync(
        Guid learnerProfileId, Guid conceptId, bool correct, Guid? cellId, CancellationToken cancellationToken);
}
