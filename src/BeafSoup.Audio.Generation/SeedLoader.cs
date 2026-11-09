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
}
