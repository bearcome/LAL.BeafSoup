using BeafSoup.Localization.EntityFrameworkCore;
using BeafSoup.SharedKernel;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeafSoup.Localization.EntityFrameworkCore.Tests;

/// <summary>临时 SQLite 文件库：每个测试实例独立文件，避免用例间相互污染。</summary>
public sealed class TempLocalizationDatabase : IAsyncDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"l10n-{Guid.NewGuid():N}.db");

    public async Task<LocalizationDbContext> CreateAndMigrateAsync()
    {
        var context = CreateContext();
        await context.Database.MigrateAsync();
        return context;
    }

    public LocalizationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocalizationDbContext>()
            .UseSqlite($"Data Source={_path}", sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Localization"))
            .Options;
        return new LocalizationDbContext(options);
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
        return ValueTask.CompletedTask;
    }
}

public class LocalizationPersistenceTests
{
    [Fact]
    public async Task 本地化文本按键与语言存储_修订号往返一致()
    {
        await using var database = new TempLocalizationDatabase();
        var text = LocalizedText.Restore(
            SemanticKey.Of("sentence.greeting.001"), LanguageCode.Of("de-DE"), "Guten Tag!", 3);

        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.SemanticKeys.Add(LocalizationPersistenceMapper.ToRow(SemanticKey.Of("sentence.greeting.001"), "content", DateTime.UtcNow));
            context.LocalizedTexts.Add(LocalizationPersistenceMapper.ToRow(text));
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var row = await read.LocalizedTexts.SingleAsync();
        LocalizationPersistenceMapper.ToDomain(row).Should().Be(text);
    }

    [Fact]
    public async Task 同一键同一语言只允许一行_同键跨语言允许()
    {
        await using var database = new TempLocalizationDatabase();
        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.SemanticKeys.AddRange(
                LocalizationPersistenceMapper.ToRow(SemanticKey.Of("sentence.greeting.001"), "content", DateTime.UtcNow),
                LocalizationPersistenceMapper.ToRow(SemanticKey.Of("sentence.greeting.002"), "content", DateTime.UtcNow));
            context.LocalizedTexts.Add(LocalizationPersistenceMapper.ToRow(
                LocalizedText.Create(SemanticKey.Of("sentence.greeting.001"), LanguageCode.Of("zh-CN"), "你好！")));
            await context.SaveChangesAsync();

            // 同键不同语言允许
            await using var crossLanguage = database.CreateContext();
            crossLanguage.LocalizedTexts.Add(LocalizationPersistenceMapper.ToRow(
                LocalizedText.Create(SemanticKey.Of("sentence.greeting.002"), LanguageCode.Of("zh-CN"), "你好！")));
            await crossLanguage.SaveChangesAsync();

            // 同键同语言必须被主键拒绝
            await using var duplicate = database.CreateContext();
            duplicate.LocalizedTexts.Add(LocalizationPersistenceMapper.ToRow(
                LocalizedText.Create(SemanticKey.Of("sentence.greeting.001"), LanguageCode.Of("zh-CN"), "重复")));
            var act = () => duplicate.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [Fact]
    public async Task 音色角色配置按语言与角色存储()
    {
        await using var database = new TempLocalizationDatabase();
        var configs = VoiceRoleConfig.EnsureCompleteSet(LanguageCode.Of("de-DE"), new[]
        {
            VoiceRoleConfig.Create(LanguageCode.Of("de-DE"), VoiceRole.Male, "Max", "de-DE-ConradNeural"),
            VoiceRoleConfig.Create(LanguageCode.Of("de-DE"), VoiceRole.Female, "Mia", "de-DE-KatjaNeural"),
            VoiceRoleConfig.Create(LanguageCode.Of("de-DE"), VoiceRole.Child, "Lars", "de-DE-KasperNeural"),
        });

        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.VoiceRoleConfigs.AddRange(configs.Select(LocalizationPersistenceMapper.ToRow));
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var restored = (await read.VoiceRoleConfigs.ToListAsync())
            .Select(LocalizationPersistenceMapper.ToDomain)
            .ToArray();
        restored.Should().BeEquivalentTo(configs);
    }
}
