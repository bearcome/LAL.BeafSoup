using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Exercises.EntityFrameworkCore;

public sealed class ExerciseInstanceRepository : IExerciseInstanceRepository
{
    private readonly ExercisesDbContext _context;

    public ExerciseInstanceRepository(ExercisesDbContext context) => _context = context;

    public async Task<ExerciseInstance?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _context.ExerciseInstances.FindAsync([id.ToString()], cancellationToken);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<ExerciseInstance>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var keys = ids.Select(i => i.ToString()).ToArray();
        return (await _context.ExerciseInstances.Where(e => keys.Contains(e.Id)).ToListAsync(cancellationToken))
            .Select(ToDomain)
            .ToArray();
    }

    public Task InsertAsync(ExerciseInstance instance, CancellationToken cancellationToken)
    {
        _context.ExerciseInstances.Add(ToRow(instance));
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);

    private static ExerciseInstanceRow ToRow(ExerciseInstance instance)
        => new()
        {
            Id = instance.Id.ToString(),
            TypeKey = instance.TypeKey,
            PayloadVersion = instance.PayloadVersion,
            PayloadJson = instance.PayloadJson,
            Difficulty = instance.Difficulty,
            HintKey = instance.HintKey,
        };

    private static ExerciseInstance ToDomain(ExerciseInstanceRow row)
        => new(Guid.Parse(row.Id), row.TypeKey, row.PayloadVersion, row.PayloadJson, row.Difficulty, row.HintKey);
}
