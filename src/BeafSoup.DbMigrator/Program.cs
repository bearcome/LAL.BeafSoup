using System.Text.Json;
using BeafSoup.Audio.Generation;
using BeafSoup.Content.EntityFrameworkCore;
using BeafSoup.Localization.EntityFrameworkCore;
using BeafSoup.Vocabulary.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BeafSoup.DbMigrator;

/// <summary>数据库迁移与种子填充工具（第 2 阶段）。命令：migrate / seed / reset。</summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var command = args.FirstOrDefault(a => !a.StartsWith('-')) ?? "seed";
        if (command is not ("migrate" or "seed" or "reset"))
        {
            Console.Error.WriteLine($"未知命令 '{command}'（可用：migrate / seed / reset）。");
            return 1;
        }

        var config = LoadConfig();
        var connectionString = ValueOf(args, "--connection") ?? config.ConnectionString;
        var seedDirectory = ValueOf(args, "--seed-dir") ?? config.SeedDirectory;
        var force = args.Contains("--force");

        try
        {
            switch (command)
            {
                case "migrate":
                    await MigrationRunner.MigrateAsync(connectionString);
                    return 0;
                case "seed":
                    await DatabaseSeeder.SeedAsync(connectionString, seedDirectory, replaceExisting: force);
                    return 0;
                default:
                    MigrationRunner.DeleteDatabaseFiles(connectionString);
                    await DatabaseSeeder.SeedAsync(connectionString, seedDirectory, replaceExisting: false);
                    return 0;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"失败：{ex.Message}");
            return 1;
        }
    }

    private static MigratorConfig LoadConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return new MigratorConfig("Data Source=beafsoup.db", "data/seed");
        var json = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(path));
        return new MigratorConfig(
            json.TryGetProperty("connectionString", out var cs) ? cs.GetString()! : "Data Source=beafsoup.db",
            json.TryGetProperty("seedDirectory", out var seed) ? seed.GetString()! : "data/seed");
    }

    private static string? ValueOf(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private sealed record MigratorConfig(string ConnectionString, string SeedDirectory);
}
