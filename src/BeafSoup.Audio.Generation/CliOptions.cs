namespace BeafSoup.Audio.Generation;

public sealed record CliOptions(string Command, bool DryRun, bool Force, string? SecretsPath, string ConfigPath)
{
    public static CliOptions Parse(string[] args)
    {
        var command = args.FirstOrDefault(a => !a.StartsWith('-')) ?? "generate";
        if (command is not ("generate" or "validate"))
            throw new ArgumentException($"未知命令 '{command}'（可用：generate / validate）。");
        var dryRun = args.Contains("--dry-run");
        var force = args.Contains("--force");
        var configPath = ValueOf(args, "--config") ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var secrets = ValueOf(args, "--secrets");
        return new CliOptions(command, dryRun, force, secrets, configPath);
    }

    private static string? ValueOf(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
