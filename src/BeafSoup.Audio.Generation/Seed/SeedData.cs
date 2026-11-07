namespace BeafSoup.Audio.Generation.Seed;

/// <summary>种子数据聚合：voices/concepts/sentences/dialogues/courses 五个 JSON 文件的反序列化目标。</summary>
public sealed class SeedData
{
    public List<SeedVoiceRole> VoiceRoles { get; set; } = [];
    public List<SeedConcept> Concepts { get; set; } = [];
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

/// <summary>概念：一个语义单元（单词/短语）的跨语言词条集合；键命名空间 concept.*。</summary>
public sealed class SeedConcept
{
    public string Key { get; set; } = "";
    public string PartOfSpeech { get; set; } = "";
    public string Kind { get; set; } = "Word";
    public Dictionary<string, List<SeedLexeme>> Lexemes { get; set; } = [];
}

/// <summary>词元：某概念在一种语言中的一个词典词（含同义/语体变体）。forms 为 范式名 → 槽位表层形式列表（槽位顺序即人称/数或格/数约定）。</summary>
public sealed class SeedLexeme
{
    public string Lemma { get; set; } = "";
    public string? Display { get; set; }
    public bool IsPrimary { get; set; }
    public string Register { get; set; } = "Neutral";
    public string? Article { get; set; }
    public string Cefr { get; set; } = "A1";
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, List<string>> Forms { get; set; } = [];
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

    /// <summary>词形链接：概念键 + 表层形式，须在 concepts.json 中命中某个范式槽位。</summary>
    public string? ConceptKey { get; set; }
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
