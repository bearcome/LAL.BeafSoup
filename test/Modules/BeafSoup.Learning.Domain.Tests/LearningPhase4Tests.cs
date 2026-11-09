using BeafSoup.SharedKernel;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Learning;

public class FsrsSchedulerTests
{
    private static readonly FsrsParameters Parameters = FsrsParameters.ReferenceDefaults;
    private static readonly DateTime Now = new(2026, 11, 9, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FirstRating_Good_SchedulesInitialStabilityDaysAhead()
    {
        var state = FsrsCardState.Restore(FsrsCardStatus.New, Now, 0, 0, 1);
        FsrsScheduler.ApplyReview(state, grade: 3, Now, Parameters);

        // 新卡首次 Good：稳定性 = w[2]，毕业为复习态（S ≥ 1 天）
        state.Stability.Should().Be(Parameters.InitialStability(3));
        state.Status.Should().Be(FsrsCardStatus.Review);
        state.DueUtc.Should().Be(Now.AddDays(state.Stability));
    }

    [Fact]
    public void RatingsAreMonotonic_EasyExceedsGoodExceedsHard()
    {
        static double StabilityAfter(int grade)
        {
            var state = FsrsCardState.Restore(FsrsCardStatus.New, Now, 0, 0, 1);
            FsrsScheduler.ApplyReview(state, grade, Now, Parameters);
            return state.Stability;
        }
        StabilityAfter(4).Should().BeGreaterThan(StabilityAfter(3));
        StabilityAfter(3).Should().BeGreaterThan(StabilityAfter(2));
        StabilityAfter(2).Should().BeGreaterThan(StabilityAfter(1));
    }

    [Fact]
    public void Again_LapsesToLearningAndDueSoon()
    {
        var state = FsrsCardState.Restore(FsrsCardStatus.Review, Now.AddDays(-3), stability: 5, difficulty: 5, schedulingVersion: 1);
        FsrsScheduler.ApplyReview(state, grade: 1, Now, Parameters);
        state.Status.Should().Be(FsrsCardStatus.Relearning);
        state.Stability.Should().BeLessThan(5);
        state.DueUtc.Should().BeCloseTo(Now.AddDays(state.Stability), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Review_IncreasesStabilityAndMovesDueOut()
    {
        var state = FsrsCardState.Restore(FsrsCardStatus.Review, Now.AddDays(-2), stability: 3, difficulty: 4, schedulingVersion: 1);
        FsrsScheduler.ApplyReview(state, grade: 3, Now, Parameters);
        state.Stability.Should().BeGreaterThan(3);
        state.DueUtc.Should().BeAfter(Now);
    }

    [Fact]
    public void InvalidGrade_Throws()
    {
        var state = FsrsCardState.Restore(FsrsCardStatus.New, Now, 0, 0, 1);
        var act = () => FsrsScheduler.ApplyReview(state, grade: 5, Now, Parameters);
        act.Should().Throw<DomainException>().Where(e => e.Code == LearningErrorCodes.InvalidRating);
    }

    [Fact]
    public void Retrievability_DecreasesWithElapsedDays()
    {
        FsrsScheduler.Retrievability(0, stability: 5).Should().Be(1);
        FsrsScheduler.Retrievability(5, stability: 5).Should().BeLessThan(1);
        FsrsScheduler.Retrievability(10, stability: 5).Should().BeLessThan(FsrsScheduler.Retrievability(5, 5));
    }
}

public class LearnerProgressStateTests
{
    private static readonly DateTime Day1 = new(2026, 11, 9, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RecordMistake_DeductsHeart_DownToZero()
    {
        var state = LearnerProgressState.Create(Guid.NewGuid(), Day1);
        for (var i = 0; i < LearnerProgressState.MaxHearts + 3; i++)
            state.RecordMistake(Day1.AddMinutes(i));
        state.Hearts.Should().Be(0);
    }

    [Fact]
    public void HeartsRegenerate_AfterRegenPeriod()
    {
        var state = LearnerProgressState.Create(Guid.NewGuid(), Day1);
        state.RecordMistake(Day1.AddMinutes(1));
        state.RecordMistake(Day1.AddMinutes(2));
        state.Hearts.Should().Be(LearnerProgressState.MaxHearts - 2);
        // 30 分钟恢复周期：恢复 1 心，且不足一个周期的时间不提前恢复
        state.ApplyRegen(Day1.AddMinutes(2 + 29));
        state.Hearts.Should().Be(LearnerProgressState.MaxHearts - 2);
        state.ApplyRegen(Day1.AddMinutes(2 + 30));
        state.Hearts.Should().Be(LearnerProgressState.MaxHearts - 1);
    }

    [Fact]
    public void Streak_ContinuesAcrossConsecutiveDays_ResetsAfterGap()
    {
        var state = LearnerProgressState.Create(Guid.NewGuid(), Day1);
        state.RecordActivity(Day1);                    // 第 1 天
        state.RecordActivity(Day1.AddHours(3));        // 同日多次 → 连胜不变
        state.DayStreak.Should().Be(1);
        state.RecordActivity(Day1.AddDays(1));         // 连续第 2 天
        state.DayStreak.Should().Be(2);
        state.RecordActivity(Day1.AddDays(3));         // 间隔一天 → 重置
        state.DayStreak.Should().Be(1);
    }

    [Fact]
    public void ProgressRecord_OnlyCorrectAnswersAwardXp()
    {
        var profileId = Guid.NewGuid();
        var activities = new[]
        {
            LearningActivity.Create(Guid.NewGuid(), "a1", profileId, LearningActivityType.GradedAnswer, Day1, isCorrect: true),
            LearningActivity.Create(Guid.NewGuid(), "a2", profileId, LearningActivityType.GradedAnswer, Day1, isCorrect: false),
            LearningActivity.Create(Guid.NewGuid(), "a3", profileId, LearningActivityType.LessonCompleted, Day1),
        };
        var record = ProgressRecord.FromActivities(profileId, activities,
            new Dictionary<LearningActivityType, int>
            {
                [LearningActivityType.GradedAnswer] = 10,
                [LearningActivityType.LessonCompleted] = 20,
            });
        record.Xp.Should().Be(30); // 答错不奖励，但也不扣已得奖励
        record.CompletedLessons.Should().Be(1);
    }
}
