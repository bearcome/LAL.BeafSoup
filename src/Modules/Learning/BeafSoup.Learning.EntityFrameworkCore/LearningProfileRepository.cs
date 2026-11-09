using BeafSoup.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Learning.EntityFrameworkCore;

public sealed class LearningProfileRepository : ILearningProfileRepository
{
    private readonly LearningDbContext _context;

    public LearningProfileRepository(LearningDbContext context) => _context = context;

    public async Task<LearnerProfile?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _context.LearnerProfiles.FindAsync([id.ToString()], cancellationToken);
        return row is null ? null : LearningPersistenceMapper.ToDomain(row);
    }

    public async Task<IReadOnlyList<LearnerProfile>> FindByUserAsync(Guid userId, CancellationToken cancellationToken)
        => (await _context.LearnerProfiles.Where(p => p.UserId == userId.ToString()).ToListAsync(cancellationToken))
            .Select(LearningPersistenceMapper.ToDomain)
            .ToArray();

    public Task InsertAsync(LearnerProfile profile, CancellationToken cancellationToken)
    {
        _context.LearnerProfiles.Add(LearningPersistenceMapper.ToRow(profile));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(LearnerProfile profile, CancellationToken cancellationToken)
    {
        var row = await _context.LearnerProfiles.FindAsync([profile.Id.ToString()], cancellationToken)
            ?? throw new DomainException(LearningErrorCodes.EmptyDedupeKey, $"学习配置 {profile.Id} 不存在。");
        var updated = LearningPersistenceMapper.ToRow(profile);
        row.TargetRetentionRate = updated.TargetRetentionRate;
        row.ThemePreference = updated.ThemePreference;
        row.InflectionDisplay = updated.InflectionDisplay;
        row.InspectorTrigger = updated.InspectorTrigger;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
