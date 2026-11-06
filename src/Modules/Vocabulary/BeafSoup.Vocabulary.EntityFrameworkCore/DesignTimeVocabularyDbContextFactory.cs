using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BeafSoup.Vocabulary.EntityFrameworkCore;

/// <summary>设计时工厂：仅供 dotnet ef 生成迁移使用，连接串不会被真实打开。</summary>
public sealed class DesignTimeVocabularyDbContextFactory : IDesignTimeDbContextFactory<VocabularyDbContext>
{
    public VocabularyDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VocabularyDbContext>()
            .UseSqlite(
                "Data Source=design-time.db",
                sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Vocabulary"))
            .Options;
        return new VocabularyDbContext(options);
    }
}
