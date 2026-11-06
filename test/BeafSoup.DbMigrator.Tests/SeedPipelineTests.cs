using BeafSoup.Content.EntityFrameworkCore;
using BeafSoup.Localization.EntityFrameworkCore;
using BeafSoup.Vocabulary.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeafSoup.DbMigrator.Tests;

/// <summary>种子入库管线的端到端验收：真实 data/seed → 校验 → 迁移 → 入库。</summary>
public class SeedPipelineTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"migrator-{Guid.NewGuid():N}");
    private string _databasePath = "";

    private string ConnectionString => $"Data Source={_databasePath}";

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _databasePath = Path.Combine(_directory, "seed-test.db");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
        return Task.CompletedTask;
    }

    private static string LocateSeedDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "data", "seed")))
            directory = directory.Parent;
        return directory is null
            ? throw new DirectoryNotFoundException("找不到仓库 data/seed 目录。")
            : Path.Combine(directory.FullName, "data", "seed");
    }

    [Fact]
    public async Task 空库执行种子_三模块数据全部入库且可往返读取()
    {
        await DatabaseSeeder.SeedAsync(ConnectionString, LocateSeedDirectory(), replaceExisting: false);

        await using (var context = new LocalizationDbContext(
            MigrationRunner.CreateOptions<LocalizationDbContext>(ConnectionString, "__EFMigrationsHistory_Localization")))
        {
            (await context.LocalizedTexts.CountAsync()).Should().BeGreaterThan(0);
            (await context.VoiceRoleConfigs.CountAsync()).Should().Be(3); // 每语言恰好男/女/儿童三角色
        }

        await using (var context = new VocabularyDbContext(
            MigrationRunner.CreateOptions<VocabularyDbContext>(ConnectionString, "__EFMigrationsHistory_Vocabulary")))
        {
            var lemmas = await context.Lemmas.ToListAsync();
            lemmas.Should().HaveCount(3); // 种子：每种数据 3 条
            lemmas.Select(VocabularyPersistenceMapper.ToDomain).Should().OnlyContain(l => l.Forms.Count >= 1);
        }

        await using (var context = new ContentDbContext(
            MigrationRunner.CreateOptions<ContentDbContext>(ConnectionString, "__EFMigrationsHistory_Content")))
        {
            (await context.Sentences.CountAsync()).Should().Be(3);
            (await context.Dialogues.CountAsync()).Should().Be(3);
            (await context.Courses.CountAsync()).Should().Be(1);
        }
    }

    [Fact]
    public async Task 非空库重复种子跳过_强制替换后数据不重复()
    {
        var seedDirectory = LocateSeedDirectory();
        await DatabaseSeeder.SeedAsync(ConnectionString, seedDirectory, replaceExisting: false);

        await DatabaseSeeder.SeedAsync(ConnectionString, seedDirectory, replaceExisting: false); // 应跳过
        await DatabaseSeeder.SeedAsync(ConnectionString, seedDirectory, replaceExisting: true); // 清空后重填

        await using var context = new LocalizationDbContext(
            MigrationRunner.CreateOptions<LocalizationDbContext>(ConnectionString, "__EFMigrationsHistory_Localization"));
        var duplicateKeys = await context.LocalizedTexts
            .GroupBy(t => new { t.Key, t.LanguageCode })
            .Where(g => g.Count() > 1)
            .CountAsync();
        duplicateKeys.Should().Be(0);
    }
}
