using Microsoft.Extensions.Options;

namespace BeafSoup.Learning;

/// <summary>复习应用服务：到期卡优先 + 每日新卡预算；新卡由客户端（持有概念词典）挑选候选概念入队，模块边界不跨域查词表。</summary>
public sealed class ReviewAppService : IReviewAppService
{
    private readonly IReviewCardRepository _cards;
    private readonly IFormMasteryRepository _formMastery;
    private readonly ILearningActivityRepository _activities;
    private readonly ILearnerProgressStateRepository _states;
    private readonly IFsrsParameterProvider _fsrsParameters;
    private readonly LearningGamificationOptions _options;
    private readonly TimeProvider _timeProvider;

    public ReviewAppService(
        IReviewCardRepository cards,
        IFormMasteryRepository formMastery,
        ILearningActivityRepository activities,
        ILearnerProgressStateRepository states,
        IFsrsParameterProvider fsrsParameters,
        IOptions<LearningGamificationOptions> options,
        TimeProvider timeProvider)
    {
        _cards = cards;
        _formMastery = formMastery;
        _activities = activities;
        _states = states;
        _fsrsParameters = fsrsParameters;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ReviewQueueItemDto>> GetDueQueueAsync(
        Guid learnerProfileId, int limit, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var due = await _cards.GetDueCardsAsync(learnerProfileId, now, limit, cancellationToken);
        return due.Select(c => new ReviewQueueItemDto(c.ConceptId, c.Fsrs.DueUtc, c.Fsrs.Status is FsrsCardStatus.New)).ToArray();
    }

    /// <summary>把客户端挑选的候选概念入队为新卡（受当日剩余预算限制；已有卡的概念跳过）。</summary>
    public async Task<int> EnqueueNewCardsAsync(
        Guid learnerProfileId, IReadOnlyList<Guid> candidateConceptIds, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var today = now.Date;
        var ratedToday = await _cards.CountRatedSinceAsync(learnerProfileId, today, cancellationToken);
        var budget = Math.Max(0, _options.NewCardsPerDay - ratedToday);
        var inserted = 0;
        foreach (var conceptId in candidateConceptIds.Distinct())
        {
            if (inserted >= budget)
                break;
            if (await _cards.FindAsync(learnerProfileId, conceptId, cancellationToken) is not null)
                continue;
            var card = new ReviewCard(learnerProfileId, conceptId);
            card.MarkDueNow(now); // 新卡立即可学
            await _cards.InsertAsync(card, cancellationToken);
            inserted++;
        }
        if (inserted > 0)
            await _cards.SaveChangesAsync(cancellationToken);
        return inserted;
    }

    public async Task<ProgressSnapshotDto> RateAsync(
        Guid learnerProfileId, RateReviewRequest request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // 概念卡：以概念为唯一复习单位，词元/形态不重复建卡
        var card = await _cards.FindAsync(learnerProfileId, request.ConceptId, cancellationToken)
            ?? new ReviewCard(learnerProfileId, request.ConceptId);
        var parameters = _fsrsParameters.GetParameters(targetRetentionRate: 0.9);
        card.Rate(request.Rating, now, parameters);
        if (await _cards.FindAsync(learnerProfileId, request.ConceptId, cancellationToken) is null)
            await _cards.InsertAsync(card, cancellationToken);
        else
            await _cards.UpdateAsync(card, cancellationToken);

        // 形态掌握度：仅当结果明确关联 ParadigmCellId 时更新；泛理解不推断词形掌握
        if (request.CellId is not null)
        {
            var mastery = await _formMastery.FindAsync(learnerProfileId, request.CellId.Value, cancellationToken)
                ?? new FormMastery(learnerProfileId, request.CellId.Value);
            mastery.RecordOutcome(request.IsCorrect, now);
            if (await _formMastery.FindAsync(learnerProfileId, request.CellId.Value, cancellationToken) is null)
                await _formMastery.InsertAsync(mastery, cancellationToken);
            else
                await _formMastery.UpdateAsync(mastery, cancellationToken);
        }

        // 复习事实：幂等（重试不重复计经验/连胜）
        var byId = await _activities.FindAsync(request.ActivityId, cancellationToken);
        if (byId is null)
        {
            await _activities.InsertAsync(LearningActivity.Create(
                request.ActivityId, $"review:{request.ActivityId}", learnerProfileId,
                LearningActivityType.ReviewSubmitted, now, isCorrect: request.IsCorrect), cancellationToken);
            var state = await _states.FindAsync(learnerProfileId, cancellationToken)
                ?? LearnerProgressState.Create(learnerProfileId, now);
            if (request.IsCorrect)
                state.RecordActivity(now);
            else
                state.RecordMistake(now);
            if (await _states.FindAsync(learnerProfileId, cancellationToken) is null)
                await _states.InsertAsync(state, cancellationToken);
            else
                await _states.UpdateAsync(state, cancellationToken);
        }

        await _cards.SaveChangesAsync(cancellationToken);

        var activities = await _activities.GetByProfileAsync(learnerProfileId, cancellationToken);
        var progressState = await _states.FindAsync(learnerProfileId, cancellationToken);
        return ProgressSnapshotFactory.Create(learnerProfileId, activities, progressState, _options, now);
    }
}
