namespace BeafSoup.Learning;

/// <summary>只读进度投影：经验值/课时完成度仅由去重后的学习活动推导；连胜/红心来自游戏化状态。不是独立写入的聚合，不能跨用户共享。</summary>
public sealed record ProgressRecord(Guid LearnerProfileId, int Xp, int CompletedLessons)
{
    public static ProgressRecord FromActivities(Guid learnerProfileId, IReadOnlyList<LearningActivity> activities, IReadOnlyDictionary<LearningActivityType, int> xpByType)
    {
        // 只有判对的作答产生经验；答错/无效活动只计入事实，不奖励（不因之前错误减少已得奖励）
        var xp = activities.Where(a => a.IsCorrect).Sum(a => xpByType.GetValueOrDefault(a.Type));
        var lessons = activities.Count(a => a.Type == LearningActivityType.LessonCompleted);
        return new ProgressRecord(learnerProfileId, xp, lessons);
    }
}
