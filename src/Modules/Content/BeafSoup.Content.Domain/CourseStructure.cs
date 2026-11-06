using BeafSoup.SharedKernel;

namespace BeafSoup.Content;

/// <summary>课时中的习题引用：题型由注册表键解析（第 4 阶段由 Exercises 模块 handler 校验），跨模块仅存 ID + 键。</summary>
public sealed record ExerciseRef(Guid ExerciseInstanceId, string ExerciseTypeKey);

public sealed class LessonContent
{
    private readonly List<ExerciseRef> _refs = [];
    public IReadOnlyList<ExerciseRef> Refs => _refs;

    public void AddExercise(Guid exerciseInstanceId, string exerciseTypeKey)
    {
        if (string.IsNullOrWhiteSpace(exerciseTypeKey))
            throw new DomainException(ContentErrorCodes.EmptyExerciseTypeKey, "题型注册键不能为空。");
        _refs.Add(new ExerciseRef(exerciseInstanceId, exerciseTypeKey.Trim()));
    }
}

public sealed class Lesson
{
    public string Id { get; }
    public SemanticKey TitleKey { get; }
    public LessonContent Content { get; } = new();

    public Lesson(string id, SemanticKey titleKey)
    {
        Id = SlugId.Validate(id, nameof(id));
        TitleKey = titleKey;
    }
}

public sealed class SkillUnit
{
    public string Id { get; }
    public SemanticKey TitleKey { get; }
    private readonly List<Lesson> _lessons = [];
    public IReadOnlyList<Lesson> Lessons => _lessons;

    public SkillUnit(string id, SemanticKey titleKey)
    {
        Id = SlugId.Validate(id, nameof(id));
        TitleKey = titleKey;
    }

    public void AddLesson(Lesson lesson) => _lessons.Add(lesson);
}

/// <summary>课程聚合根：课程 → 技能单元 → 课时；学习者进度不写回课程定义。</summary>
public sealed class Course
{
    public string Id { get; }
    public SemanticKey TitleKey { get; }
    private readonly List<SkillUnit> _units = [];
    public IReadOnlyList<SkillUnit> Units => _units;

    public Course(string id, SemanticKey titleKey)
    {
        Id = SlugId.Validate(id, nameof(id));
        TitleKey = titleKey;
    }

    public void AddSkillUnit(SkillUnit unit) => _units.Add(unit);
}
