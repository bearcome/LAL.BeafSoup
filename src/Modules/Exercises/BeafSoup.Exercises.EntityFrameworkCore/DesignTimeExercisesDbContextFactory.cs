using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BeafSoup.Exercises.EntityFrameworkCore;

/// <summary>设计时工厂：仅供 dotnet ef 生成迁移使用，连接串不会被真实打开。</summary>
public sealed class DesignTimeExercisesDbContextFactory : IDesignTimeDbContextFactory<ExercisesDbContext>
{
    public ExercisesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ExercisesDbContext>()
            .UseSqlite(
                "Data Source=design-time.db",
                sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Exercises"))
            .Options;
        return new ExercisesDbContext(options);
    }
}
