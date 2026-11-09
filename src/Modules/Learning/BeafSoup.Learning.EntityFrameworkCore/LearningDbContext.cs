using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Learning.EntityFrameworkCore;

/// <summary>Learning 模块持久化上下文：学习配置、复习卡（FSRS 状态）、形态掌握度、学习活动事实、游戏化状态。迁移历史表独立（ADR-005）。</summary>
public sealed class LearningDbContext : DbContext
{
    public LearningDbContext(DbContextOptions<LearningDbContext> options)
        : base(options)
    {
    }

    public DbSet<LearnerProfileRow> LearnerProfiles => Set<LearnerProfileRow>();
    public DbSet<ReviewCardRow> ReviewCards => Set<ReviewCardRow>();
    public DbSet<FormMasteryRow> FormMasteryRecords => Set<FormMasteryRow>();
    public DbSet<LearningActivityRow> LearningActivities => Set<LearningActivityRow>();
    public DbSet<LearnerProgressStateRow> LearnerProgressStates => Set<LearnerProgressStateRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearnerProfileRow>(static b =>
        {
            b.ToTable("learner_profiles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36);
            b.Property(x => x.UserId).HasMaxLength(36);
            b.HasIndex(x => x.UserId);
            b.Property(x => x.BaseLanguage).HasMaxLength(20);
            b.Property(x => x.TargetLanguage).HasMaxLength(20);
            b.Property(x => x.ThemePreference).HasMaxLength(16);
            b.Property(x => x.InflectionDisplay).HasMaxLength(16);
            b.Property(x => x.InspectorTrigger).HasMaxLength(16);
        });

        modelBuilder.Entity<ReviewCardRow>(static b =>
        {
            b.ToTable("review_cards");
            b.HasKey(x => new { x.ProfileId, x.ConceptId });
            b.Property(x => x.ProfileId).HasMaxLength(36);
            b.Property(x => x.ConceptId).HasMaxLength(36);
            b.HasIndex(x => x.DueUtc);
        });

        modelBuilder.Entity<FormMasteryRow>(static b =>
        {
            b.ToTable("form_mastery");
            b.HasKey(x => new { x.ProfileId, x.CellId });
            b.Property(x => x.ProfileId).HasMaxLength(36);
            b.Property(x => x.CellId).HasMaxLength(36);
        });

        modelBuilder.Entity<LearningActivityRow>(static b =>
        {
            b.ToTable("learning_activities");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36);
            b.Property(x => x.DedupeKey).HasMaxLength(128);
            b.Property(x => x.ProfileId).HasMaxLength(36);
            b.Property(x => x.Type).HasMaxLength(32);
            // 去重键按学习配置唯一：重试/重放不重复计分
            b.HasIndex(x => new { x.ProfileId, x.DedupeKey }).IsUnique();
            b.HasIndex(x => x.ProfileId);
        });

        modelBuilder.Entity<LearnerProgressStateRow>(static b =>
        {
            b.ToTable("learner_progress_states");
            b.HasKey(x => x.ProfileId);
            b.Property(x => x.ProfileId).HasMaxLength(36);
        });
    }
}
