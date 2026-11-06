using System.Text.Json;

namespace BeafSoup.Audio.Generation.Generation;

public static class ManifestWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Write(string outputDirectory, IReadOnlyList<PlannedAsset> assets, IReadOnlyList<string> writtenFiles)
    {
        Directory.CreateDirectory(outputDirectory);
        var manifest = new
        {
            generatedAtUtc = DateTime.UtcNow.ToString("O"),
            files = writtenFiles,
            assets = assets.Select(a => new
            {
                path = a.RelativePath,
                kind = a.SourceKind,
                sourceKey = a.SourceKey,
                voiceRole = a.Role.ToString(),
                voiceName = a.VoiceName,
                ttsText = a.TtsText,
            }),
        };
        File.WriteAllText(Path.Combine(outputDirectory, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions));
    }
}
