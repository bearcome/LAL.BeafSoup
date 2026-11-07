using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Content.EntityFrameworkCore;

/// <summary>内容模块持久化上下文：句子、分词、短语标注、对话、短文与课程树。与其他模块共享同一 SQLite 数据库文件，迁移历史表独立（见 ADR-005）。</summary>
public sealed class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options)
        : base(options)
    {
    }

    public DbSet<SentenceRow> Sentences => Set<SentenceRow>();
    public DbSet<SentenceTokenizationRow> SentenceTokenizations => Set<SentenceTokenizationRow>();
    public DbSet<SentenceTokenRow> SentenceTokens => Set<SentenceTokenRow>();
    public DbSet<SentenceSpanLinkRow> SentenceSpanLinks => Set<SentenceSpanLinkRow>();
    public DbSet<DialogueRow> Dialogues => Set<DialogueRow>();
    public DbSet<DialogueLineRow> DialogueLines => Set<DialogueLineRow>();
    public DbSet<EssayRow> Essays => Set<EssayRow>();
    public DbSet<EssaySentenceRow> EssaySentences => Set<EssaySentenceRow>();
    public DbSet<CourseRow> Courses => Set<CourseRow>();
    public DbSet<SkillUnitRow> SkillUnits => Set<SkillUnitRow>();
    public DbSet<LessonRow> Lessons => Set<LessonRow>();
    public DbSet<LessonExerciseRow> LessonExercises => Set<LessonExerciseRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SentenceRow>(static b =>
        {
            b.ToTable("sentences");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(64);
            b.Property(x => x.Key).HasMaxLength(128);
            b.HasMany(x => x.Tokenizations).WithOne().HasForeignKey(t => t.SentenceId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Tokenizations).AutoInclude();
        });

        modelBuilder.Entity<SentenceTokenizationRow>(static b =>
        {
            b.ToTable("sentence_tokenizations");
            b.HasKey(x => new { x.SentenceId, x.LanguageCode });
            b.Property(x => x.SentenceId).HasMaxLength(64);
            b.Property(x => x.LanguageCode).HasMaxLength(20);
            b.HasMany(x => x.Tokens).WithOne().HasForeignKey(t => new { t.SentenceId, t.LanguageCode }).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Tokens).AutoInclude();
        });

        modelBuilder.Entity<SentenceTokenRow>(static b =>
        {
            b.ToTable("sentence_tokens");
            b.HasKey(x => new { x.SentenceId, x.LanguageCode, x.Ordinal });
            b.Property(x => x.SentenceId).HasMaxLength(64);
            b.Property(x => x.LanguageCode).HasMaxLength(20);
            b.Property(x => x.Kind).HasMaxLength(24);
            b.HasIndex(x => x.FormCellId);
        });

        modelBuilder.Entity<SentenceSpanLinkRow>(static b =>
        {
            b.ToTable("sentence_span_links");
            b.HasKey(x => x.Id);
            b.Property(x => x.SentenceId).HasMaxLength(64);
            b.Property(x => x.LanguageCode).HasMaxLength(20);
            b.Property(x => x.LexicalEntryKey).HasMaxLength(128);
            b.HasIndex(x => new { x.SentenceId, x.LanguageCode, x.TextRevision });
        });

        modelBuilder.Entity<DialogueRow>(static b =>
        {
            b.ToTable("dialogues");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(64);
            b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.DialogueId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Lines).AutoInclude();
        });

        modelBuilder.Entity<DialogueLineRow>(static b =>
        {
            b.ToTable("dialogue_lines");
            b.HasKey(x => new { x.DialogueId, x.Ordinal });
            b.Property(x => x.DialogueId).HasMaxLength(64);
            b.Property(x => x.SpeakerLabel).HasMaxLength(64);
            b.Property(x => x.SentenceId).HasMaxLength(64);
            b.Property(x => x.VoiceRole).HasMaxLength(16);
        });

        modelBuilder.Entity<EssayRow>(static b =>
        {
            b.ToTable("essays");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(64);
            b.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.EssayId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Items).AutoInclude();
        });

        modelBuilder.Entity<EssaySentenceRow>(static b =>
        {
            b.ToTable("essay_sentences");
            b.HasKey(x => new { x.EssayId, x.Ordinal });
            b.Property(x => x.EssayId).HasMaxLength(64);
            b.Property(x => x.SentenceId).HasMaxLength(64);
        });

        modelBuilder.Entity<CourseRow>(static b =>
        {
            b.ToTable("courses");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(64);
            b.Property(x => x.TitleKey).HasMaxLength(128);
            b.HasMany(x => x.Units).WithOne().HasForeignKey(u => u.CourseId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Units).AutoInclude();
        });

        modelBuilder.Entity<SkillUnitRow>(static b =>
        {
            b.ToTable("skill_units");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(64);
            b.Property(x => x.CourseId).HasMaxLength(64);
            b.Property(x => x.TitleKey).HasMaxLength(128);
            b.HasMany(x => x.Lessons).WithOne().HasForeignKey(l => l.SkillUnitId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Lessons).AutoInclude();
        });

        modelBuilder.Entity<LessonRow>(static b =>
        {
            b.ToTable("lessons");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(64);
            b.Property(x => x.SkillUnitId).HasMaxLength(64);
            b.Property(x => x.TitleKey).HasMaxLength(128);
            b.HasMany(x => x.Exercises).WithOne().HasForeignKey(e => e.LessonId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Exercises).AutoInclude();
        });

        modelBuilder.Entity<LessonExerciseRow>(static b =>
        {
            b.ToTable("lesson_exercises");
            b.HasKey(x => new { x.LessonId, x.Ordinal });
            b.Property(x => x.LessonId).HasMaxLength(64);
            b.Property(x => x.ExerciseTypeKey).HasMaxLength(64);
        });
    }
}
