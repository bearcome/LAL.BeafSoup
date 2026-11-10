using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.Content;

/// <summary>内容 API：课程树与课时内容（登录用户可读；内容定义不含用户进度）。</summary>
[ApiController]
[Authorize]
[Route("api/content")]
public sealed class ContentController(ICourseCatalogQuery catalog) : ControllerBase
{
    public const string CoursesPath = "/api/content/courses";
    public const string LessonPath = "/api/content/lessons/{lessonId}";

    [HttpGet("courses")]
    public async Task<ActionResult<IReadOnlyList<CourseTreeDto>>> GetCourses(CancellationToken cancellationToken)
        => Ok(await catalog.GetCourseTreeAsync(cancellationToken));

    [HttpGet("lessons/{lessonId}")]
    public async Task<ActionResult<LessonContentDto>> GetLesson(
        string lessonId,
        CancellationToken cancellationToken)
        => await catalog.GetLessonContentAsync(lessonId, cancellationToken) is { } lesson
            ? Ok(lesson)
            : NotFound();
}
