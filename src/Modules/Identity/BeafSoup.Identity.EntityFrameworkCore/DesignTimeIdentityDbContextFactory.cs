using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BeafSoup.Identity.EntityFrameworkCore;

/// <summary>设计时工厂：仅供 dotnet ef 生成迁移使用，连接串不会被真实打开。</summary>
public sealed class DesignTimeIdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(
                "Data Source=design-time.db",
                sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Identity"))
            .Options;
        return new IdentityDbContext(options);
    }
}
