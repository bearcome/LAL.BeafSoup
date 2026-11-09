namespace BeafSoup.Exercises;

/// <summary>习题实例仓储：负载持久化抽象（实现在 EntityFrameworkCore）。</summary>
public interface IExerciseInstanceRepository
{
    Task<ExerciseInstance?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExerciseInstance>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken);
    Task InsertAsync(ExerciseInstance instance, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
