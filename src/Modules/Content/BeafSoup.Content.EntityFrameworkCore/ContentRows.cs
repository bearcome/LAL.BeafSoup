namespace BeafSoup.Content.EntityFrameworkCore;

/// <summary>句子行：全文只存语义键，本地化文本在 Localization 模块。</summary>
public sealed class SentenceRow
{
    public string Id { get; set; } = "";
    public string Key { get; set; } = "";
    public List<SentenceTokenizationRow> Tokenizations { get; set; } = [];
}

/// <summary>分词结果行：绑定文本修订号，主键 (sentence_id, language_code)。</summary>
public sealed class SentenceTokenizationRow
{
    public string SentenceId { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public int TextRevision { get; set; }
    public List<SentenceTokenRow> Tokens { get; set; } = [];
}

/// <summary>分词片段行：零基 Unicode scalar 起点/长度，不复制原文；Ordinal 保序。</summary>
public sealed class SentenceTokenRow
{
    public string SentenceId { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public int Ordinal { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string Kind { get; set; } = "";
    public Guid? InflectionFormId { get; set; }
}

/// <summary>跨词元短语标注行：索引绑定文本修订号，修订失配即失效。</summary>
public sealed class SentenceSpanLinkRow
{
    public long Id { get; set; }
    public string SentenceId { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public int TextRevision { get; set; }
    public int StartTokenIndex { get; set; }
    public int TokenCount { get; set; }
    public string LexicalEntryKey { get; set; } = "";
}

/// <summary>对话行集合：有序台词列表。</summary>
public sealed class DialogueRow
{
    public string Id { get; set; } = "";
    public List<DialogueLineRow> Lines { get; set; } = [];
}

/// <summary>对话行：说话人标签仅展示、绝不进入 TTS 输入；Ordinal 保序。</summary>
public sealed class DialogueLineRow
{
    public string DialogueId { get; set; } = "";
    public int Ordinal { get; set; }
    public string SpeakerLabel { get; set; } = "";
    public string SentenceId { get; set; } = "";
    public string VoiceRole { get; set; } = "";
}

/// <summary>短文行：有序句子列表，音频由句子资产前端拼接。</summary>
public sealed class EssayRow
{
    public string Id { get; set; } = "";
    public List<EssaySentenceRow> Items { get; set; } = [];
}

/// <summary>短文句子引用行；Ordinal 保序。</summary>
public sealed class EssaySentenceRow
{
    public string EssayId { get; set; } = "";
    public int Ordinal { get; set; }
    public string SentenceId { get; set; } = "";
}

/// <summary>课程行：课程 → 技能单元 → 课时；学习者进度不写回课程定义。</summary>
public sealed class CourseRow
{
    public string Id { get; set; } = "";
    public string TitleKey { get; set; } = "";
    public List<SkillUnitRow> Units { get; set; } = [];
}

/// <summary>技能单元行；Ordinal 在课程内保序。</summary>
public sealed class SkillUnitRow
{
    public string Id { get; set; } = "";
    public string CourseId { get; set; } = "";
    public int Ordinal { get; set; }
    public string TitleKey { get; set; } = "";
    public List<LessonRow> Lessons { get; set; } = [];
}

/// <summary>课时行；Ordinal 在单元内保序。</summary>
public sealed class LessonRow
{
    public string Id { get; set; } = "";
    public string SkillUnitId { get; set; } = "";
    public int Ordinal { get; set; }
    public string TitleKey { get; set; } = "";
    public List<LessonExerciseRow> Exercises { get; set; } = [];
}

/// <summary>课时习题引用行：题型由注册表键解析，跨模块仅存 ID + 键；Ordinal 保序。</summary>
public sealed class LessonExerciseRow
{
    public string LessonId { get; set; } = "";
    public int Ordinal { get; set; }
    public Guid ExerciseInstanceId { get; set; }
    public string ExerciseTypeKey { get; set; } = "";
}
