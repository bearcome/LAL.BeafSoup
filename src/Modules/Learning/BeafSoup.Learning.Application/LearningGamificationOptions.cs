namespace BeafSoup.Learning;

/// <summary>经验与新卡预算规则（宿主配置；答对才得经验，答错只记录事实不奖励）。</summary>
public sealed class LearningGamificationOptions
{
    public const string SectionName = "Learning";

    public int XpPerGradedAnswer { get; set; } = 10;
    public int XpPerLessonCompleted { get; set; } = 20;
    public int XpPerReviewSubmitted { get; set; } = 5;
    public int NewCardsPerDay { get; set; } = 15;
}
