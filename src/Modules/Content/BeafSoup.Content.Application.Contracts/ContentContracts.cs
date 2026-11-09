namespace BeafSoup.Content;

public sealed record LessonRefDto(string LessonId, string TitleKey);
public sealed record SkillUnitDto(string UnitId, string TitleKey, IReadOnlyList<LessonRefDto> Lessons);
public sealed record CourseTreeDto(string CourseId, string TitleKey, IReadOnlyList<SkillUnitDto> Units);

/// <summary>课时习题引用：跨模块仅存实例 ID + 题型注册表键；负载由 Exercises 模块按实例供给（校验后下发）。</summary>
public sealed record LessonExerciseRefDto(Guid ExerciseInstanceId, string ExerciseTypeKey);
public sealed record LessonContentDto(string LessonId, string TitleKey, IReadOnlyList<LessonExerciseRefDto> Exercises);

/// <summary>课程目录查询：课程树（含本地化标题键）与课时内容引用。</summary>
public interface ICourseCatalogQuery
{
    Task<IReadOnlyList<CourseTreeDto>> GetCourseTreeAsync(CancellationToken cancellationToken);
    Task<LessonContentDto?> GetLessonContentAsync(string lessonId, CancellationToken cancellationToken);
}
