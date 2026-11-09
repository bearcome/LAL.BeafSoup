namespace BeafSoup.Learning;

public sealed record LearnerProfileDto(
    Guid Id,
    string BaseLanguage,
    string TargetLanguage,
    double TargetRetentionRate,
    string ThemePreference,
    string InflectionDisplay,
    string InspectorTrigger);

public sealed record CreateLearnerProfileRequest(string BaseLanguage, string TargetLanguage);

public sealed record UpdateProfilePreferencesRequest(
    string? Theme,
    double? TargetRetentionRate,
    string? InflectionDisplay,
    string? InspectorTrigger);

/// <summary>进度快照：经验/课时来自学习活动投影；红心/连胜来自游戏化状态（单一组合点，客户端不各自推导）。</summary>
public sealed record ProgressSnapshotDto(
    Guid LearnerProfileId,
    int Xp,
    int CompletedLessons,
    int Hearts,
    int DayStreak,
    bool LearnedToday);

public sealed record SubmitActivityRequest(
    Guid ActivityId,
    string DedupeKey,
    LearningActivityType Type,
    Guid? ExerciseInstanceId,
    Guid? LessonId,
    bool IsCorrect);

public sealed record SubmitActivityResponse(bool AcceptedAsNew, ProgressSnapshotDto Progress);

public sealed record ReviewQueueItemDto(Guid ConceptId, DateTime DueUtc, bool IsNew);

public sealed record RateReviewRequest(
    Guid ConceptId, int Rating, Guid ActivityId, Guid? CellId, bool IsCorrect);

/// <summary>学习配置应用服务：每个账户可拥有多个语言学习配置；所有权校验在 API 层（UserId 只能来自验证过的身份）。</summary>
public interface ILearningProfileAppService
{
    Task<IReadOnlyList<LearnerProfileDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken);
    Task<LearnerProfileDto> CreateAsync(Guid userId, CreateLearnerProfileRequest request, CancellationToken cancellationToken);
    Task<LearnerProfileDto> UpdatePreferencesAsync(Guid profileId, UpdateProfilePreferencesRequest request, CancellationToken cancellationToken);
}

/// <summary>学习活动提交：幂等（ActivityId + 按配置唯一的 DedupeKey）；重试返回既有结果，不重复变更进度。</summary>
public interface ILearningActivityAppService
{
    Task<SubmitActivityResponse> SubmitAsync(Guid learnerProfileId, SubmitActivityRequest request, CancellationToken cancellationToken);
}

/// <summary>复习队列：到期卡片优先（FSRS DueUtc），其后按每日新卡预算（默认 15）。</summary>
public interface IReviewAppService
{
    Task<IReadOnlyList<ReviewQueueItemDto>> GetDueQueueAsync(Guid learnerProfileId, int limit, CancellationToken cancellationToken);

    /// <summary>把客户端挑选的候选概念入队为新卡（受当日剩余预算限制；已有卡的概念跳过）；返回实际入队数。</summary>
    Task<int> EnqueueNewCardsAsync(Guid learnerProfileId, IReadOnlyList<Guid> candidateConceptIds, CancellationToken cancellationToken);

    Task<ProgressSnapshotDto> RateAsync(Guid learnerProfileId, RateReviewRequest request, CancellationToken cancellationToken);
}

public interface ILearningProgressQuery
{
    Task<ProgressSnapshotDto> GetProgressAsync(Guid learnerProfileId, CancellationToken cancellationToken);
}

public sealed record EnqueueNewCardsRequest(IReadOnlyList<Guid> ConceptIds);
public sealed record EnqueuedNewCardsResponse(int Inserted);
