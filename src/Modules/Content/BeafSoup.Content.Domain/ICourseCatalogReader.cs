namespace BeafSoup.Content;

public sealed record LessonRefEntry(string LessonId, string TitleKey);
public sealed record SkillUnitEntry(string UnitId, string TitleKey, IReadOnlyList<LessonRefEntry> Lessons);
public sealed record CourseTreeEntry(string CourseId, string TitleKey, IReadOnlyList<SkillUnitEntry> Units);

/// <summary>课时习题引用（跨模块仅存实例 ID + 题型键；负载在 Exercises 模块）。</summary>
public sealed record LessonExerciseRefEntry(Guid ExerciseInstanceId, string ExerciseTypeKey);
public sealed record LessonContentEntry(string LessonId, string TitleKey, IReadOnlyList<LessonExerciseRefEntry> Exercises);

/// <summary>课程目录读模型查询（实现在 EntityFrameworkCore；只读）。</summary>
public interface ICourseCatalogReader
{
    Task<IReadOnlyList<CourseTreeEntry>> GetCourseTreeAsync(CancellationToken cancellationToken);
    Task<LessonContentEntry?> GetLessonContentAsync(string lessonId, CancellationToken cancellationToken);
}
