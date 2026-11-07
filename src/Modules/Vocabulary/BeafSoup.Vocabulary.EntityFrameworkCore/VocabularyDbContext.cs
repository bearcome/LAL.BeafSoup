using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Vocabulary.EntityFrameworkCore;

/// <summary>词汇模块持久化上下文：概念、词元与范式槽位。与其他模块共享同一 SQLite 数据库文件，迁移历史表独立（见 ADR-005）。</summary>
public sealed class VocabularyDbContext : DbContext
{
    public VocabularyDbContext(DbContextOptions<VocabularyDbContext> options)
        : base(options)
    {
    }

    public DbSet<LexicalConceptRow> LexicalConcepts => Set<LexicalConceptRow>();
    public DbSet<LexemeRow> Lexemes => Set<LexemeRow>();
    public DbSet<LexemeTagRow> LexemeTags => Set<LexemeTagRow>();
    public DbSet<ParadigmCellRow> ParadigmCells => Set<ParadigmCellRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LexicalConceptRow>(static b =>
        {
            b.ToTable("lexical_concepts");
            b.HasKey(x => x.Id);
            b.Property(x => x.ConceptKey).HasMaxLength(128);
            b.Property(x => x.PartOfSpeech).HasMaxLength(16);
            b.Property(x => x.Kind).HasMaxLength(8);
            b.HasIndex(x => x.ConceptKey).IsUnique();
            b.HasMany(x => x.Lexemes).WithOne().HasForeignKey(l => l.ConceptId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Lexemes).AutoInclude();
        });

        modelBuilder.Entity<LexemeRow>(static b =>
        {
            b.ToTable("lexemes");
            b.HasKey(x => x.Id);
            b.Property(x => x.LanguageCode).HasMaxLength(20);
            b.Property(x => x.Lemma).HasMaxLength(128);
            b.Property(x => x.DisplayWord).HasMaxLength(128);
            b.Property(x => x.Register).HasMaxLength(16);
            b.Property(x => x.Article).HasMaxLength(8);
            b.Property(x => x.Cefr).HasMaxLength(4);
            b.HasIndex(x => new { x.ConceptId, x.LanguageCode, x.Ordinal }).IsUnique();
            b.HasMany(x => x.Tags).WithOne().HasForeignKey(t => t.LexemeId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Cells).WithOne().HasForeignKey(c => c.LexemeId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Tags).AutoInclude();
            b.Navigation(x => x.Cells).AutoInclude();
        });

        modelBuilder.Entity<LexemeTagRow>(static b =>
        {
            b.ToTable("lexeme_tags");
            b.HasKey(x => new { x.LexemeId, x.Ordinal });
            b.Property(x => x.Tag).HasMaxLength(64);
        });

        modelBuilder.Entity<ParadigmCellRow>(static b =>
        {
            b.ToTable("paradigm_cells");
            b.HasKey(x => x.Id);
            b.Property(x => x.ParadigmKey).HasMaxLength(64);
            b.HasIndex(x => x.ParadigmKey);
            b.Property(x => x.Surface).HasMaxLength(128);
        });
    }
}
