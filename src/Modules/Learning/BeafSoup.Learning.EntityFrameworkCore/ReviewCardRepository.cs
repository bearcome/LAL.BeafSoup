using BeafSoup.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Learning.EntityFrameworkCore;

public sealed class ReviewCardRepository : IReviewCardRepository
{
    private readonly LearningDbContext _context;

    public ReviewCardRepository(LearningDbContext context) => _context = context;

    public async Task<ReviewCard?> FindAsync(Guid learnerProfileId, Guid conceptId, CancellationToken cancellationToken)
    {
        var row = await _context.ReviewCards.FindAsync([learnerProfileId.ToString(), conceptId.ToString()], cancellationToken);
        return row is null ? null : LearningPersistenceMapper.ToDomain(row);
    }

    public async Task<IReadOnlyList<ReviewCard>> GetDueCardsAsync(Guid learnerProfileId, DateTime nowUtc, int limit, CancellationToken cancellationToken)
        => (await _context.ReviewCards
                .Where(c => c.ProfileId == learnerProfileId.ToString() && c.DueUtc <= nowUtc)
                .OrderBy(c => c.DueUtc)
                .Take(limit)
                .ToListAsync(cancellationToken))
            .Select(LearningPersistenceMapper.ToDomain)
            .ToArray();

    public Task<int> CountRatedSinceAsync(Guid learnerProfileId, DateTime sinceUtc, CancellationToken cancellationToken)
        => _context.ReviewCards.CountAsync(
            c => c.ProfileId == learnerProfileId.ToString() && c.DueUtc >= sinceUtc && c.Status != nameof(FsrsCardStatus.New),
            cancellationToken);

    public Task InsertAsync(ReviewCard card, CancellationToken cancellationToken)
    {
        _context.ReviewCards.Add(LearningPersistenceMapper.ToRow(card));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ReviewCard card, CancellationToken cancellationToken)
    {
        var row = await _context.ReviewCards.FindAsync(
            [card.LearnerProfileId.ToString(), card.ConceptId.ToString()], cancellationToken)
            ?? throw new DomainException(SharedKernelErrorCodes.InvalidSemanticKey, $"复习卡不存在。");
        var updated = LearningPersistenceMapper.ToRow(card);
        row.Status = updated.Status;
        row.DueUtc = updated.DueUtc;
        row.Stability = updated.Stability;
        row.Difficulty = updated.Difficulty;
        row.SchedulingVersion = updated.SchedulingVersion;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
