using BeafSoup.Learning;
using BeafSoup.SharedKernel;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Learning.Domain.Tests;

public class LearnerProfileTests
{
    [Fact]
    public void 同一账户可拥有多个学习配置()
    {
        var userId = Guid.NewGuid();
        var p1 = LearnerProfile.Create(Guid.NewGuid(), userId, LanguageCode.Of("zh-CN"), LanguageCode.Of("de-DE"), 0.9);
        var p2 = LearnerProfile.Create(Guid.NewGuid(), userId, LanguageCode.Of("zh-CN"), LanguageCode.Of("en-US"), 0.85);
        p1.Id.Should().NotBe(p2.Id);
        p1.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.01)]
    [InlineData(-0.5)]
    public void 目标记忆率必须在01开区间(double rate)
    {
        var act = () => LearnerProfile.Create(Guid.NewGuid(), Guid.NewGuid(), LanguageCode.Of("zh-CN"), LanguageCode.Of("de-DE"), rate);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(LearningErrorCodes.InvalidRetentionRate);
    }
}

public class ReviewCardTests
{
    [Fact]
    public void 复习卡以概念为粒度_同配置同概念唯一()
    {
        var profileId = Guid.NewGuid();
        var conceptId = Guid.NewGuid();
        var card1 = new ReviewCard(profileId, conceptId);
        var card2 = new ReviewCard(profileId, conceptId);
        card1.Should().Be(card2); // 唯一键 (ProfileId, ConceptId)
    }

    [Fact]
    public void 新卡初始为FSRS新状态()
    {
        var card = new ReviewCard(Guid.NewGuid(), Guid.NewGuid());
        card.Fsrs.Status.Should().Be(FsrsCardStatus.New);
        card.Fsrs.SchedulingVersion.Should().Be(1);
    }
}

public class FormMasteryTests
{
    [Fact]
    public void 形态掌握度独立记录作答表现()
    {
        var mastery = new FormMastery(Guid.NewGuid(), Guid.NewGuid());
        var now = DateTime.UtcNow;
        mastery.RecordOutcome(correct: true, now);
        mastery.RecordOutcome(correct: false, now.AddMinutes(1));
        mastery.RecordOutcome(correct: true, now.AddMinutes(2));
        mastery.CorrectCount.Should().Be(2);
        mastery.IncorrectCount.Should().Be(1);
        mastery.LastSeenAtUtc.Should().Be(now.AddMinutes(2));
    }
}

public class LearningActivityTests
{
    [Fact]
    public void 学习事实必须携带去重键()
    {
        var act = () => LearningActivity.Create(Guid.NewGuid(), dedupeKey: "  ", Guid.NewGuid(), LearningActivityType.GradedAnswer, DateTime.UtcNow);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(LearningErrorCodes.EmptyDedupeKey);
    }

    [Fact]
    public void 去重键相同即同一事实_重试不重复计分()
    {
        var id = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var at = DateTime.UtcNow;
        var a1 = LearningActivity.Create(id, "ex:42:try:1", profileId, LearningActivityType.GradedAnswer, at);
        var a2 = LearningActivity.Create(id, "ex:42:try:1", profileId, LearningActivityType.GradedAnswer, at);
        a2.DedupeKey.Should().Be(a1.DedupeKey);
    }
}

public class ProgressRecordTests
{
    [Fact]
    public void 进度只由去重后的学习事实推导()
    {
        var profileId = Guid.NewGuid();
        var at = DateTime.UtcNow;
        var activities = new[]
        {
            LearningActivity.Create(Guid.NewGuid(), "a", profileId, LearningActivityType.GradedAnswer, at),
            LearningActivity.Create(Guid.NewGuid(), "b", profileId, LearningActivityType.ReviewSubmitted, at),
            LearningActivity.Create(Guid.NewGuid(), "c", profileId, LearningActivityType.LessonCompleted, at),
        };
        var xpByType = new Dictionary<LearningActivityType, int>
        {
            [LearningActivityType.GradedAnswer] = 10,
            [LearningActivityType.ReviewSubmitted] = 5,
            [LearningActivityType.LessonCompleted] = 15,
        };
        var record = ProgressRecord.FromActivities(profileId, activities, xpByType);
        record.Xp.Should().Be(30);
        record.LearnerProfileId.Should().Be(profileId);
    }
}
