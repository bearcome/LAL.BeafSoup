using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Vocabulary.EntityFrameworkCore;

/// <summary>词汇模块持久化上下文：词元、词形与语法分析。与其他模块共享同一 SQLite 数据库文件，迁移历史表独立（见 ADR-005）。</summary>
public sealed class VocabularyDbContext : DbContext
{
    public VocabularyDbContext(DbContextOptions<VocabularyDbContext> options)
        : base(options)
    {
    }

    public DbSet<LemmaRow> Lemmas => Set<LemmaRow>();
    public DbSet<LemmaTagRow> LemmaTags => Set<LemmaTagRow>();
    public DbSet<InflectionFormRow> InflectionForms => Set<InflectionFormRow>();
    public DbSet<FormAnalysisRow> FormAnalyses => Set<FormAnalysisRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LemmaRow>(static b =>
        {
            b.ToTable("lemmas");
            b.HasKey(x => x.Id);
            b.Property(x => x.LanguageCode).HasMaxLength(20);
            b.Property(x => x.LemmaKey).HasMaxLength(128);
            b.Property(x => x.PartOfSpeech).HasMaxLength(16);
            b.Property(x => x.Article).HasMaxLength(8);
            b.Property(x => x.Cefr).HasMaxLength(4);
            b.HasIndex(x => x.LemmaKey).IsUnique();
            b.HasMany(x => x.Tags).WithOne().HasForeignKey(t => t.LemmaId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Forms).WithOne().HasForeignKey(f => f.LemmaId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Tags).AutoInclude();
            b.Navigation(x => x.Forms).AutoInclude();
        });

        modelBuilder.Entity<LemmaTagRow>(static b =>
        {
            b.ToTable("lemma_tags");
            b.HasKey(x => new { x.LemmaId, x.Ordinal });
            b.Property(x => x.Tag).HasMaxLength(64);
        });

        modelBuilder.Entity<InflectionFormRow>(static b =>
        {
            b.ToTable("inflection_forms");
            b.HasKey(x => x.Id);
            b.Property(x => x.SurfaceKey).HasMaxLength(128);
            b.HasIndex(x => x.SurfaceKey);
            b.HasMany(x => x.Analyses).WithOne().HasForeignKey(a => a.FormId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Analyses).AutoInclude();
        });

        modelBuilder.Entity<FormAnalysisRow>(static b =>
        {
            b.ToTable("form_analyses");
            b.HasKey(x => new { x.FormId, x.Ordinal });
            b.Property(x => x.Case).HasMaxLength(16);
            b.Property(x => x.Number).HasMaxLength(16);
            b.Property(x => x.Person).HasMaxLength(16);
            b.Property(x => x.Tense).HasMaxLength(16);
            b.Property(x => x.Gender).HasMaxLength(16);
            b.Property(x => x.Degree).HasMaxLength(16);
        });
    }
}
