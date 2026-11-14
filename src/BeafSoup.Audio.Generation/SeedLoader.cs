using System.Text.Json;
using BeafSoup.Audio.Generation.Seed;

namespace BeafSoup.Audio.Generation;

public static class SeedLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static SeedData Load(string seedDirectory)
    {
        if (!Directory.Exists(seedDirectory))
            throw new DirectoryNotFoundException($"种子目录不存在：{seedDirectory}");
        var seed = new SeedData
        {
            VoiceRoles = LoadFile<List<SeedVoiceRole>>(seedDirectory, "voices.json"),
            Concepts = LoadFile<List<SeedConcept>>(seedDirectory, "concepts.json"),
            Sentences = LoadFile<List<SeedSentence>>(seedDirectory, "sentences.json"),
            Dialogues = LoadFile<List<SeedDialogue>>(seedDirectory, "dialogues.json"),
            Essays = LoadOptionalFile<List<SeedEssay>>(seedDirectory, "essays.json") ?? [],
            Courses = LoadFile<List<SeedCourse>>(seedDirectory, "courses.json"),
            UiTexts = LoadFile<List<SeedUiText>>(seedDirectory, "ui-texts.json"),
            Exercises = LoadFile<List<SeedExercise>>(seedDirectory, "exercises.json"),
        };
        return seed;
    }

    private static T LoadFile<T>(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"缺少种子文件：{path}");
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"种子文件 {path} 内容为空或格式错误。");
    }

    /// <summary>可选种子文件：不存在时返回默认值（如短文数据在旧种子目录中尚未提供）。</summary>
    private static T? LoadOptionalFile<T>(string directory, string fileName) where T : class
    {
        var path = Path.Combine(directory, fileName);
        return File.Exists(path)
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
            : null;
    }
}
