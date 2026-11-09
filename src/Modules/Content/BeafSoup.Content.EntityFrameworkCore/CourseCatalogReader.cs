using Microsoft.EntityFrameworkCore;

namespace BeafSoup.Content.EntityFrameworkCore;

/// <summary>课程目录读模型实现：课程 → 技能单元 → 课时 → 习题引用（按 Ordinal 保序）。</summary>
public sealed class CourseCatalogReader : ICourseCatalogReader
{
    private readonly ContentDbContext _context;

    public CourseCatalogReader(ContentDbContext context) => _context = context;

    public async Task<IReadOnlyList<CourseTreeEntry>> GetCourseTreeAsync(CancellationToken cancellationToken)
    {
        var courses = await _context.Courses.OrderBy(c => c.Id).ToListAsync(cancellationToken);
        var units = await _context.SkillUnits.OrderBy(u => u.Ordinal).ToListAsync(cancellationToken);
        var lessons = await _context.Lessons.OrderBy(l => l.Ordinal).ToListAsync(cancellationToken);

        return courses.Select(course => new CourseTreeEntry(
            course.Id,
            course.TitleKey,
            units.Where(u => u.CourseId == course.Id).Select(unit => new SkillUnitEntry(
                unit.Id,
                unit.TitleKey,
                lessons.Where(l => l.SkillUnitId == unit.Id)
                    .Select(l => new LessonRefEntry(l.Id, l.TitleKey))
                    .ToArray())).ToArray())).ToArray();
    }

    public async Task<LessonContentEntry?> GetLessonContentAsync(string lessonId, CancellationToken cancellationToken)
    {
        var lesson = await _context.Lessons.FindAsync([lessonId], cancellationToken);
        if (lesson is null)
            return null;
        var exercises = await _context.LessonExercises
            .Where(e => e.LessonId == lessonId)
            .OrderBy(e => e.Ordinal)
            .ToListAsync(cancellationToken);
        return new LessonContentEntry(
            lesson.Id,
            lesson.TitleKey,
            exercises.Select(e => new LessonExerciseRefEntry(e.ExerciseInstanceId, e.ExerciseTypeKey)).ToArray());
    }
}
