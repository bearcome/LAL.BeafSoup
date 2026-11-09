using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Identity.EntityFrameworkCore;

/// <summary>Identity 模块持久化上下文：账户、身份关联、凭据散列、注册事实事件、会话。与其他模块共享同一 SQLite 文件，迁移历史表独立（ADR-005）。</summary>
public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccountRow> UserAccounts => Set<UserAccountRow>();
    public DbSet<UserIdentityLinkRow> UserIdentityLinks => Set<UserIdentityLinkRow>();
    public DbSet<UserCredentialRow> UserCredentials => Set<UserCredentialRow>();
    public DbSet<UserRegisteredEventRow> UserRegisteredEvents => Set<UserRegisteredEventRow>();
    public DbSet<UserSessionRow> UserSessions => Set<UserSessionRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccountRow>(static b =>
        {
            b.ToTable("user_accounts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36);
            b.Property(x => x.Status).HasMaxLength(16);
            b.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<UserIdentityLinkRow>(static b =>
        {
            b.ToTable("user_identity_links");
            b.HasKey(x => new { x.Provider, x.ProviderUserId });
            b.Property(x => x.Provider).HasMaxLength(32);
            b.Property(x => x.ProviderUserId).HasMaxLength(256);
            b.Property(x => x.UserId).HasMaxLength(36);
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<UserCredentialRow>(static b =>
        {
            b.ToTable("user_credentials");
            b.HasKey(x => new { x.Provider, x.ProviderUserId });
            b.Property(x => x.Provider).HasMaxLength(32);
            b.Property(x => x.ProviderUserId).HasMaxLength(256);
            b.Property(x => x.Hash).HasMaxLength(512);
        });

        modelBuilder.Entity<UserRegisteredEventRow>(static b =>
        {
            b.ToTable("user_registered_events");
            b.HasKey(x => x.EventId);
            b.Property(x => x.EventId).HasMaxLength(36);
            b.Property(x => x.UserId).HasMaxLength(36);
        });

        modelBuilder.Entity<UserSessionRow>(static b =>
        {
            b.ToTable("user_sessions");
            b.HasKey(x => x.SessionId);
            b.Property(x => x.SessionId).HasMaxLength(36);
            b.Property(x => x.UserId).HasMaxLength(36);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.LastHeartbeatAtUtc);
        });
    }
}
