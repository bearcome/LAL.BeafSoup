namespace BeafSoup.Learning;

/// <summary>只读进度投影：经验值/课时完成度仅由去重后的学习活动推导；不是独立写入的聚合，不能跨用户共享。连胜/红心等维度随第 4 阶段学习功能补充。</summary>
public sealed record ProgressRecord(Guid LearnerProfileId, int Xp, int CompletedLessons)
{
    public static ProgressRecord FromActivities(Guid learnerProfileId, IReadOnlyList<LearningActivity> activities, IReadOnlyDictionary<LearningActivityType, int> xpByType)
    {
        var xp = activities.Sum(a => xpByType.GetValueOrDefault(a.Type));
        var lessons = activities.Count(a => a.Type == LearningActivityType.LessonCompleted);
        return new ProgressRecord(learnerProfileId, xp, lessons);
    }
}
