using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BeafSoup.Learning.EntityFrameworkCore;

/// <summary>设计时工厂：仅供 dotnet ef 生成迁移使用，连接串不会被真实打开。</summary>
public sealed class DesignTimeLearningDbContextFactory : IDesignTimeDbContextFactory<LearningDbContext>
{
    public LearningDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseSqlite(
                "Data Source=design-time.db",
                sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Learning"))
            .Options;
        return new LearningDbContext(options);
    }
}
