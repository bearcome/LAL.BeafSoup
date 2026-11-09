using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Exercises.EntityFrameworkCore;

/// <summary>Exercises 模块持久化上下文：习题实例（版本化 JSON 负载）。迁移历史表独立（ADR-005）。</summary>
public sealed class ExercisesDbContext : DbContext
{
    public ExercisesDbContext(DbContextOptions<ExercisesDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExerciseInstanceRow> ExerciseInstances => Set<ExerciseInstanceRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExerciseInstanceRow>(static b =>
        {
            b.ToTable("exercise_instances");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36);
            b.Property(x => x.TypeKey).HasMaxLength(64);
            b.Property(x => x.PayloadJson);
            b.Property(x => x.HintKey).HasMaxLength(128);
            b.HasIndex(x => x.TypeKey);
        });
    }
}
