using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Localization.EntityFrameworkCore;

/// <summary>本地化模块持久化上下文：语义键注册表、本地化文本、音色角色配置。与其他模块共享同一 SQLite 数据库文件，迁移历史表独立（见 ADR-005）。</summary>
public sealed class LocalizationDbContext : DbContext
{
    public LocalizationDbContext(DbContextOptions<LocalizationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SemanticKeyRegistration> SemanticKeys => Set<SemanticKeyRegistration>();
    public DbSet<LocalizedTextRow> LocalizedTexts => Set<LocalizedTextRow>();
    public DbSet<VoiceRoleConfigRow> VoiceRoleConfigs => Set<VoiceRoleConfigRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SemanticKeyRegistration>(static b =>
        {
            b.ToTable("semantic_keys");
            b.HasKey(x => x.Key);
            b.Property(x => x.Key).HasMaxLength(128);
            b.Property(x => x.Domain).HasMaxLength(32);
        });

        modelBuilder.Entity<LocalizedTextRow>(static b =>
        {
            b.ToTable("localized_texts");
            b.HasKey(x => new { x.Key, x.LanguageCode });
            b.Property(x => x.Key).HasMaxLength(128);
            b.Property(x => x.LanguageCode).HasMaxLength(20);
            b.Property(x => x.Text);
            b.Property(x => x.LemmaKey).HasMaxLength(128);
            b.HasIndex(x => x.LemmaKey);
        });

        modelBuilder.Entity<VoiceRoleConfigRow>(static b =>
        {
            b.ToTable("voice_role_configs");
            b.HasKey(x => new { x.LanguageCode, x.Role });
            b.Property(x => x.LanguageCode).HasMaxLength(20);
            b.Property(x => x.Role).HasMaxLength(16);
            b.Property(x => x.AssetFileToken).HasMaxLength(32);
            b.Property(x => x.TtsVoiceName).HasMaxLength(64);
        });
    }
}
