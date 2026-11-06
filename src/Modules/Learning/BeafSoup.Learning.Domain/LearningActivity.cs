using BeafSoup.SharedKernel;

namespace BeafSoup.Learning;

/// <summary>不可变学习事实：已判分作答/复习/课时完成；去重键保证重试不会重复计分。</summary>
public sealed record LearningActivity
{
    public Guid Id { get; }
    public string DedupeKey { get; }
    public Guid LearnerProfileId { get; }
    public LearningActivityType Type { get; }
    public DateTime OccurredAtUtc { get; }
    public Guid? ExerciseInstanceId { get; }
    public Guid? LessonId { get; }

    private LearningActivity(Guid id, string dedupeKey, Guid learnerProfileId, LearningActivityType type, DateTime occurredAtUtc, Guid? exerciseInstanceId, Guid? lessonId)
        => (Id, DedupeKey, LearnerProfileId, Type, OccurredAtUtc, ExerciseInstanceId, LessonId)
         = (id, dedupeKey, learnerProfileId, type, occurredAtUtc, exerciseInstanceId, lessonId);

    public static LearningActivity Create(Guid id, string dedupeKey, Guid learnerProfileId, LearningActivityType type, DateTime occurredAtUtc, Guid? exerciseInstanceId = null, Guid? lessonId = null)
    {
        if (string.IsNullOrWhiteSpace(dedupeKey))
            throw new DomainException(LearningErrorCodes.EmptyDedupeKey, "学习事实必须携带去重键。");
        return new LearningActivity(id, dedupeKey.Trim(), learnerProfileId, type, occurredAtUtc, exerciseInstanceId, lessonId);
    }
}
