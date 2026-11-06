using BeafSoup.Content.EntityFrameworkCore;
using BeafSoup.Localization.EntityFrameworkCore;
using BeafSoup.Vocabulary.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.DbMigrator;

/// <summary>按 ADR-005 应用三个模块的 EF 迁移：共享一个 SQLite 数据库文件，各模块迁移历史表独立。</summary>
public static class MigrationRunner
{
    public static async Task MigrateAsync(string connectionString)
    {
        // WAL 一次性开启并持久化；备份前须 checkpoint（见 ADR-005）
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode=WAL;";
            await command.ExecuteNonQueryAsync();
        }

        await using (var context = new LocalizationDbContext(CreateOptions<LocalizationDbContext>(
            connectionString, "__EFMigrationsHistory_Localization")))
            await context.Database.MigrateAsync();

        await using (var context = new VocabularyDbContext(CreateOptions<VocabularyDbContext>(
            connectionString, "__EFMigrationsHistory_Vocabulary")))
            await context.Database.MigrateAsync();

        await using (var context = new ContentDbContext(CreateOptions<ContentDbContext>(
            connectionString, "__EFMigrationsHistory_Content")))
            await context.Database.MigrateAsync();

        Console.WriteLine("迁移完成：Localization / Vocabulary / Content。");
    }

    public static DbContextOptions<T> CreateOptions<T>(string connectionString, string migrationsHistoryTable)
        where T : DbContext
        => new DbContextOptionsBuilder<T>()
            .UseSqlite(connectionString, sqlite => sqlite.MigrationsHistoryTable(migrationsHistoryTable))
            .Options;

    /// <summary>删除 SQLite 数据库文件（含 WAL/SHM 伴随文件）；仅 reset 命令使用。</summary>
    public static void DeleteDatabaseFiles(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = Path.GetFullPath(builder.DataSource);
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = dataSource + suffix;
            if (File.Exists(path))
                File.Delete(path);
        }
        Console.WriteLine($"已删除数据库文件：{dataSource}");
    }
}
