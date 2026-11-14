namespace BeafSoup.Audio.Generation.Seed;

/// <summary>种子数据聚合：voices/concepts/sentences/dialogues/courses/ui-texts 六个 JSON 文件的反序列化目标。</summary>
public sealed class SeedData
{
    public List<SeedVoiceRole> VoiceRoles { get; set; } = [];
    public List<SeedConcept> Concepts { get; set; } = [];
    public List<SeedSentence> Sentences { get; set; } = [];
    public List<SeedDialogue> Dialogues { get; set; } = [];
    public List<SeedEssay> Essays { get; set; } = [];
    public List<SeedCourse> Courses { get; set; } = [];
    public List<SeedUiText> UiTexts { get; set; } = [];
    public List<SeedExercise> Exercises { get; set; } = [];
}

/// <summary>习题实例种子：负载为原始 JSON，correctCell 引用在入库时解析为稳定 ParadigmCellId。</summary>
public sealed class SeedExercise
{
    public string Id { get; set; } = "";
    public string TypeKey { get; set; } = "";
    public int Difficulty { get; set; } = 1;
    public System.Text.Json.JsonElement Payload { get; set; }
}

/// <summary>界面文案：domain=ui 的语义键 + N 语言版本；同一机制服务学习内容与界面文案。</summary>
public sealed class SeedUiText
{
    public string Key { get; set; } = "";
    public List<SeedUiTextLocalization> Localizations { get; set; } = [];
}

public sealed class SeedUiTextLocalization
{
    public string Language { get; set; } = "";
    public string Text { get; set; } = "";
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

/// <summary>短文种子：有序句子列表；音频由句子资产前端拼接，无独立 TTS 文件。</summary>
public sealed class SeedEssay
{
    public string Id { get; set; } = "";
    public List<string> SentenceIds { get; set; } = [];
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
    public List<SeedTitleLocalization> TitleLocalizations { get; set; } = [];
    public List<SeedSkillUnit> Units { get; set; } = [];
}

/// <summary>标题/名称的 N 语言版本：与句子全文同一套 localized_texts 机制。</summary>
public sealed class SeedTitleLocalization
{
    public string Language { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class SeedSkillUnit
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public List<SeedTitleLocalization> TitleLocalizations { get; set; } = [];
    public List<SeedLesson> Lessons { get; set; } = [];
}

public sealed class SeedLesson
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";

    /// <summary>课时习题引用：instanceId 对应 exercises.json 的习题；题型键必须与习题一致。</summary>
    public List<SeedLessonExercise> Exercises { get; set; } = [];

    public List<SeedTitleLocalization> TitleLocalizations { get; set; } = [];
}

public sealed class SeedLessonExercise
{
    public string InstanceId { get; set; } = "";
    public string TypeKey { get; set; } = "";
}
