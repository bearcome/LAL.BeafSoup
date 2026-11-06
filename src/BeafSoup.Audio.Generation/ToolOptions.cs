using System.Text.Json;

namespace BeafSoup.Audio.Generation;

public sealed class ToolOptions
{
    public required string SeedDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public Dictionary<string, string> CefrRates { get; init; } = [];
    public Dictionary<string, RoleProsodyOverride> RoleProsody { get; init; } = [];

    public sealed class RoleProsodyOverride
    {
        public string? Pitch { get; set; }
        public string? Rate { get; set; }
    }

    public static ToolOptions Load(string configPath)
    {
        var baseDir = AppContext.BaseDirectory;
        using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
        var root = doc.RootElement;
        string Resolve(string key) => Path.GetFullPath(root.GetProperty(key).GetString()!, baseDir);
        var cefrRates = root.GetProperty("cefrRates").Deserialize<Dictionary<string, string>>() ?? [];
        var prosody = root.GetProperty("roleProsody").Deserialize<Dictionary<string, RoleProsodyOverride>>() ?? [];
        return new ToolOptions
        {
            SeedDirectory = Resolve("seedDirectory"),
            OutputDirectory = Resolve("outputDirectory"),
            CefrRates = cefrRates,
            RoleProsody = prosody,
        };
    }
}

/// <summary>secret.json 的 AzureTTS 段；值只在运行时绑定，绝不进入源码/日志。</summary>
public sealed class AzureTtsOptions
{
    public string? Endpoint { get; init; }
    public required string ApiKey { get; init; }
    public required string Region { get; init; }

    public static AzureTtsOptions Load(string secretsPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(secretsPath));
        var section = doc.RootElement.GetProperty("AzureTTS");
        return new AzureTtsOptions
        {
            Endpoint = section.GetProperty("Endpoint").GetString(),
            ApiKey = section.GetProperty("ApiKey").GetString()
                ?? throw new InvalidDataException("secret.json 缺少 AzureTTS:ApiKey。"),
            Region = section.GetProperty("Region").GetString()
                ?? throw new InvalidDataException("secret.json 缺少 AzureTTS:Region。"),
        };
    }
}
