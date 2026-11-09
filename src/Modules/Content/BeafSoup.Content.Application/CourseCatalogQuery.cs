namespace BeafSoup.Content;

public sealed class CourseCatalogQuery : ICourseCatalogQuery
{
    private readonly ICourseCatalogReader _reader;

    public CourseCatalogQuery(ICourseCatalogReader reader) => _reader = reader;

    public async Task<IReadOnlyList<CourseTreeDto>> GetCourseTreeAsync(CancellationToken cancellationToken)
    {
        var tree = await _reader.GetCourseTreeAsync(cancellationToken);
        return tree.Select(course => new CourseTreeDto(
            course.CourseId,
            course.TitleKey,
            course.Units.Select(unit => new SkillUnitDto(
                unit.UnitId,
                unit.TitleKey,
                unit.Lessons.Select(lesson => new LessonRefDto(lesson.LessonId, lesson.TitleKey)).ToArray())).ToArray())).ToArray();
    }

    public async Task<LessonContentDto?> GetLessonContentAsync(string lessonId, CancellationToken cancellationToken)
    {
        var content = await _reader.GetLessonContentAsync(lessonId, cancellationToken);
        return content is null
            ? null
            : new LessonContentDto(
                content.LessonId,
                content.TitleKey,
                content.Exercises.Select(e => new LessonExerciseRefDto(e.ExerciseInstanceId, e.ExerciseTypeKey)).ToArray());
    }
}
