namespace BeafSoup.Learning;

/// <summary>学习模块持久化抽象（实现在 EntityFrameworkCore）；查询返回脱管领域对象，变更须显式 Update 写回。</summary>
public interface ILearningProfileRepository
{
    Task<LearnerProfile?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<LearnerProfile>> FindByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task InsertAsync(LearnerProfile profile, CancellationToken cancellationToken);
    Task UpdateAsync(LearnerProfile profile, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IReviewCardRepository
{
    Task<ReviewCard?> FindAsync(Guid learnerProfileId, Guid conceptId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReviewCard>> GetDueCardsAsync(Guid learnerProfileId, DateTime nowUtc, int limit, CancellationToken cancellationToken);
    /// <summary>当日已评级（非 New）的卡片数：用于每日新卡预算。</summary>
    Task<int> CountRatedSinceAsync(Guid learnerProfileId, DateTime sinceUtc, CancellationToken cancellationToken);
    Task InsertAsync(ReviewCard card, CancellationToken cancellationToken);
    Task UpdateAsync(ReviewCard card, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IFormMasteryRepository
{
    Task<FormMastery?> FindAsync(Guid learnerProfileId, Guid cellId, CancellationToken cancellationToken);
    Task InsertAsync(FormMastery formMastery, CancellationToken cancellationToken);
    Task UpdateAsync(FormMastery formMastery, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ILearningActivityRepository
{
    Task<LearningActivity?> FindAsync(Guid activityId, CancellationToken cancellationToken);
    Task<LearningActivity?> FindByDedupeKeyAsync(Guid learnerProfileId, string dedupeKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<LearningActivity>> GetByProfileAsync(Guid learnerProfileId, CancellationToken cancellationToken);
    Task InsertAsync(LearningActivity activity, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ILearnerProgressStateRepository
{
    Task<LearnerProgressState?> FindAsync(Guid learnerProfileId, CancellationToken cancellationToken);
    Task InsertAsync(LearnerProgressState state, CancellationToken cancellationToken);
    Task UpdateAsync(LearnerProgressState state, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
