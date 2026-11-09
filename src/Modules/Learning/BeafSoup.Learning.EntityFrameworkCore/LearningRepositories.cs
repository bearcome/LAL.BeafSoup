using BeafSoup.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Learning.EntityFrameworkCore;

public sealed class FormMasteryRepository : IFormMasteryRepository
{
    private readonly LearningDbContext _context;

    public FormMasteryRepository(LearningDbContext context) => _context = context;

    public async Task<FormMastery?> FindAsync(Guid learnerProfileId, Guid cellId, CancellationToken cancellationToken)
    {
        var row = await _context.FormMasteryRecords.FindAsync([learnerProfileId.ToString(), cellId.ToString()], cancellationToken);
        return row is null ? null : LearningPersistenceMapper.ToDomain(row);
    }

    public Task InsertAsync(FormMastery formMastery, CancellationToken cancellationToken)
    {
        _context.FormMasteryRecords.Add(LearningPersistenceMapper.ToRow(formMastery));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(FormMastery formMastery, CancellationToken cancellationToken)
    {
        var row = await _context.FormMasteryRecords.FindAsync(
            [formMastery.LearnerProfileId.ToString(), formMastery.CellId.ToString()], cancellationToken)
            ?? throw new DomainException(LearningErrorCodes.EmptyDedupeKey, "形态掌握度记录不存在。");
        var updated = LearningPersistenceMapper.ToRow(formMastery);
        row.CorrectCount = updated.CorrectCount;
        row.IncorrectCount = updated.IncorrectCount;
        row.LastSeenAtUtc = updated.LastSeenAtUtc;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}

public sealed class LearningActivityRepository : ILearningActivityRepository
{
    private readonly LearningDbContext _context;

    public LearningActivityRepository(LearningDbContext context) => _context = context;

    public async Task<LearningActivity?> FindAsync(Guid activityId, CancellationToken cancellationToken)
    {
        var row = await _context.LearningActivities.FindAsync([activityId.ToString()], cancellationToken);
        return row is null ? null : LearningPersistenceMapper.ToDomain(row);
    }

    public async Task<LearningActivity?> FindByDedupeKeyAsync(Guid learnerProfileId, string dedupeKey, CancellationToken cancellationToken)
    {
        var row = await _context.LearningActivities
            .FirstOrDefaultAsync(a => a.ProfileId == learnerProfileId.ToString() && a.DedupeKey == dedupeKey, cancellationToken);
        return row is null ? null : LearningPersistenceMapper.ToDomain(row);
    }

    public async Task<IReadOnlyList<LearningActivity>> GetByProfileAsync(Guid learnerProfileId, CancellationToken cancellationToken)
        => (await _context.LearningActivities
                .Where(a => a.ProfileId == learnerProfileId.ToString())
                .ToListAsync(cancellationToken))
            .Select(LearningPersistenceMapper.ToDomain)
            .ToArray();

    public Task InsertAsync(LearningActivity activity, CancellationToken cancellationToken)
    {
        _context.LearningActivities.Add(LearningPersistenceMapper.ToRow(activity));
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}

public sealed class LearnerProgressStateRepository : ILearnerProgressStateRepository
{
    private readonly LearningDbContext _context;

    public LearnerProgressStateRepository(LearningDbContext context) => _context = context;

    public async Task<LearnerProgressState?> FindAsync(Guid learnerProfileId, CancellationToken cancellationToken)
    {
        var row = await _context.LearnerProgressStates.FindAsync([learnerProfileId.ToString()], cancellationToken);
        return row is null ? null : LearningPersistenceMapper.ToDomain(row);
    }

    public Task InsertAsync(LearnerProgressState state, CancellationToken cancellationToken)
    {
        _context.LearnerProgressStates.Add(LearningPersistenceMapper.ToRow(state));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(LearnerProgressState state, CancellationToken cancellationToken)
    {
        var row = await _context.LearnerProgressStates.FindAsync([state.LearnerProfileId.ToString()], cancellationToken)
            ?? throw new DomainException(LearningErrorCodes.EmptyDedupeKey, "学习进度状态不存在。");
        var updated = LearningPersistenceMapper.ToRow(state);
        row.Hearts = updated.Hearts;
        row.LastHeartChangeAtUtc = updated.LastHeartChangeAtUtc;
        row.LastActivityDateUtc = updated.LastActivityDateUtc;
        row.DayStreak = updated.DayStreak;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
