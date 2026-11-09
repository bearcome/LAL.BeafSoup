using BeafSoup.Learning;

namespace BeafSoup.Exercises;

/// <summary>习题呈现数据：负载已经过 handler 校验；客户端按题型注册表键选择渲染组件。</summary>
public sealed record ExercisePresentationDto(
    Guid InstanceId,
    string TypeKey,
    int PayloadVersion,
    string PayloadJson,
    int Difficulty,
    string? HintKey);

public interface IExercisePresentationQuery
{
    Task<IReadOnlyList<ExercisePresentationDto>> GetPresentationsAsync(
        IReadOnlyList<Guid> instanceIds, CancellationToken cancellationToken);
}

public sealed record GradeAnswerRequest(
    Guid ActivityId,
    string DedupeKey,
    Guid LearnerProfileId,
    string AnswerJson,
    Guid? LessonId);

public sealed record GradeAnswerResponse(
    bool Correct,
    string? FeedbackKey,
    ProgressSnapshotDto Progress);

/// <summary>判分应用服务：服务端权威判分 + 幂等活动记录 + 适用的 SRS/形态掌握度供数（经复习关联声明）。</summary>
public interface IExerciseGradingAppService
{
    Task<GradeAnswerResponse> GradeAsync(
        Guid exerciseInstanceId, GradeAnswerRequest request, CancellationToken cancellationToken);
}
