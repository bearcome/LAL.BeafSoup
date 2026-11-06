namespace BeafSoup.Audio.Generation.Seed;

/// <summary>种子数据聚合：voices/lemmas/sentences/dialogues/courses 五个 JSON 文件的反序列化目标。</summary>
public sealed class SeedData
{
    public List<SeedVoiceRole> VoiceRoles { get; set; } = [];
    public List<SeedLemma> Lemmas { get; set; } = [];
    public List<SeedSentence> Sentences { get; set; } = [];
    public List<SeedDialogue> Dialogues { get; set; } = [];
    public List<SeedCourse> Courses { get; set; } = [];
}

public sealed class SeedVoiceRole
{
    public string Language { get; set; } = "";
    public string Role { get; set; } = "";
    public string AssetFileToken { get; set; } = "";
    public string TtsVoiceName { get; set; } = "";
}

public sealed class SeedLemma
{
    public string Key { get; set; } = "";
    public string Language { get; set; } = "";
    public string PartOfSpeech { get; set; } = "";
    public string? Article { get; set; }
    public string Cefr { get; set; } = "A1";
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, string> Translations { get; set; } = [];
    public List<SeedInflectionForm> Forms { get; set; } = [];
}

public sealed class SeedInflectionForm
{
    public string Key { get; set; } = "";
    public string Surface { get; set; } = "";
    public List<SeedAnalysis> Analyses { get; set; } = [];
}

public sealed class SeedAnalysis
{
    public string? Case { get; set; }
    public string? Number { get; set; }
    public string? Person { get; set; }
    public string? Tense { get; set; }
    public string? Gender { get; set; }
    public string? Degree { get; set; }
}

public sealed class SeedSentence
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public List<SeedSentenceLocalization> Localizations { get; set; } = [];
}

public sealed class SeedSentenceLocalization
{
    public string Language { get; set; } = "";
    public string Text { get; set; } = "";
    public int Revision { get; set; } = 1;

    /// <summary>空列表表示该语言尚未分词（如 zh-CN 分词处理器未实现），校验时跳过覆盖检查。</summary>
    public List<SeedToken> Tokens { get; set; } = [];
}

public sealed class SeedToken
{
    public int Start { get; set; }
    public int Length { get; set; }
    public string Kind { get; set; } = "";
    public string? FormLemmaKey { get; set; }
    public string? FormSurface { get; set; }
}

public sealed class SeedDialogue
{
    public string Id { get; set; } = "";
    public List<SeedDialogueLine> Lines { get; set; } = [];
}

public sealed class SeedDialogueLine
{
    /// <summary>说话人标签仅展示，绝不进入 TTS 输入。</summary>
    public string SpeakerLabel { get; set; } = "";
    public string SentenceId { get; set; } = "";
    public string VoiceRole { get; set; } = "";
}

public sealed class SeedCourse
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public List<SeedSkillUnit> Units { get; set; } = [];
}

public sealed class SeedSkillUnit
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public List<SeedLesson> Lessons { get; set; } = [];
}

public sealed class SeedLesson
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
}
