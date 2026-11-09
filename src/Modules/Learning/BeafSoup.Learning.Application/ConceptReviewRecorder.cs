namespace BeafSoup.Learning;

/// <summary>IConceptReviewRecorder 实现：概念卡评级 + 形态掌握度更新（不重复建卡、不产生活动）。</summary>
public sealed class ConceptReviewRecorder : IConceptReviewRecorder
{
    private readonly IReviewCardRepository _cards;
    private readonly IFormMasteryRepository _formMastery;
    private readonly IFsrsParameterProvider _fsrsParameters;
    private readonly TimeProvider _timeProvider;

    public ConceptReviewRecorder(
        IReviewCardRepository cards,
        IFormMasteryRepository formMastery,
        IFsrsParameterProvider fsrsParameters,
        TimeProvider timeProvider)
    {
        _cards = cards;
        _formMastery = formMastery;
        _fsrsParameters = fsrsParameters;
        _timeProvider = timeProvider;
    }

    public async Task RecordGradedOutcomeAsync(
        Guid learnerProfileId, Guid conceptId, bool correct, Guid? cellId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        // 概念卡唯一（profile × concept）；新卡首次评级即从 New 进入调度
        var card = await _cards.FindAsync(learnerProfileId, conceptId, cancellationToken)
            ?? new ReviewCard(learnerProfileId, conceptId);
        card.Rate(correct ? 3 : 1, now, _fsrsParameters.GetParameters(targetRetentionRate: 0.9));
        if (await _cards.FindAsync(learnerProfileId, conceptId, cancellationToken) is null)
            await _cards.InsertAsync(card, cancellationToken);
        else
            await _cards.UpdateAsync(card, cancellationToken);

        // 形态掌握度：仅明确关联 ParadigmCellId 时更新
        if (cellId is not null)
        {
            var mastery = await _formMastery.FindAsync(learnerProfileId, cellId.Value, cancellationToken)
                ?? new FormMastery(learnerProfileId, cellId.Value);
            mastery.RecordOutcome(correct, now);
            if (await _formMastery.FindAsync(learnerProfileId, cellId.Value, cancellationToken) is null)
                await _formMastery.InsertAsync(mastery, cancellationToken);
            else
                await _formMastery.UpdateAsync(mastery, cancellationToken);
        }

        await _cards.SaveChangesAsync(cancellationToken);
    }
}
