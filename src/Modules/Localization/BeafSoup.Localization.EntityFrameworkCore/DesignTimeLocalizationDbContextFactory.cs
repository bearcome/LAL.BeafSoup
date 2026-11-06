using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BeafSoup.Localization.EntityFrameworkCore;

/// <summary>设计时工厂：仅供 dotnet ef 生成迁移使用，连接串不会被真实打开。</summary>
public sealed class DesignTimeLocalizationDbContextFactory : IDesignTimeDbContextFactory<LocalizationDbContext>
{
    public LocalizationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseSqlite(
                "Data Source=design-time.db",
                sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Localization"))
            .Options;
        return new LocalizationDbContext(options);
    }
}
