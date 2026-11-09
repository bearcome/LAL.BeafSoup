using System.Text.Json;
using BeafSoup.Learning;
using BeafSoup.SharedKernel;

namespace BeafSoup.Exercises;

/// <summary>习题呈现查询：下发前逐一经 handler 校验，损坏/过期负载明确失败。</summary>
public sealed class ExercisePresentationQuery : IExercisePresentationQuery
{
    private readonly IExerciseInstanceRepository _instances;
    private readonly ExerciseRegistry _registry;

    public ExercisePresentationQuery(IExerciseInstanceRepository instances, ExerciseRegistry registry)
        => (_instances, _registry) = (instances, registry);

    public async Task<IReadOnlyList<ExercisePresentationDto>> GetPresentationsAsync(
        IReadOnlyList<Guid> instanceIds, CancellationToken cancellationToken)
    {
        var instances = await _instances.GetByIdsAsync(instanceIds, cancellationToken);
        return instances.Select(instance =>
        {
            var handler = _registry.Resolve(instance.TypeKey, instance.PayloadVersion);
            using var document = JsonDocument.Parse(instance.PayloadJson);
            handler.ValidatePayload(document.RootElement.Clone());
            return new ExercisePresentationDto(
                instance.Id, instance.TypeKey, instance.PayloadVersion, instance.PayloadJson,
                instance.Difficulty, instance.HintKey);
        }).ToArray();
    }
}

/// <summary>判分应用服务：服务端权威判分；幂等活动（重试不重复计分）；按复习关联更新概念卡与形态掌握度。</summary>
public sealed class ExerciseGradingAppService : IExerciseGradingAppService
{
    private readonly IExerciseInstanceRepository _instances;
    private readonly ExerciseRegistry _registry;
    private readonly ILearningActivityAppService _activities;
    private readonly IConceptReviewRecorder _conceptReviews;

    public ExerciseGradingAppService(
        IExerciseInstanceRepository instances,
        ExerciseRegistry registry,
        ILearningActivityAppService activities,
        IConceptReviewRecorder conceptReviews)
    {
        _instances = instances;
        _registry = registry;
        _activities = activities;
        _conceptReviews = conceptReviews;
    }

    public async Task<GradeAnswerResponse> GradeAsync(
        Guid exerciseInstanceId, GradeAnswerRequest request, CancellationToken cancellationToken)
    {
        var instance = await _instances.FindAsync(exerciseInstanceId, cancellationToken)
            ?? throw new DomainException(ExerciseErrorCodes.InvalidPayload, $"习题实例 {exerciseInstanceId} 不存在。");
        var handler = _registry.Resolve(instance.TypeKey, instance.PayloadVersion);

        using var payloadDocument = JsonDocument.Parse(instance.PayloadJson);
        var payload = payloadDocument.RootElement.Clone();
        using var answerDocument = JsonDocument.Parse(request.AnswerJson);
        var answer = answerDocument.RootElement.Clone();

        // 服务端权威判分：按题型惯例与负载声明的答案引用/可接受词形
        var grade = handler.Grade(payload, answer);

        // 幂等活动记录：经验/红心/连胜不因重试重复变更
        var submission = await _activities.SubmitAsync(request.LearnerProfileId, new SubmitActivityRequest(
            request.ActivityId, request.DedupeKey, LearningActivityType.GradedAnswer,
            exerciseInstanceId, request.LessonId, grade.Correct), cancellationToken);

        // 复习关联：明确关联概念/词形的判分结果供 SRS 与形态掌握度；泛理解题不推断
        var link = handler.TryGetReviewLink(payload);
        if (link?.ConceptKey is not null)
        {
            var conceptId = StableGuid.For($"concept:{link.ConceptKey}");
            await _conceptReviews.RecordGradedOutcomeAsync(
                request.LearnerProfileId, conceptId, grade.Correct, link.CellId, cancellationToken);
        }

        return new(grade.Correct, grade.FeedbackKey, submission.Progress);
    }
}
