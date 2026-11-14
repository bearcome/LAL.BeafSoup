using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.Content;

/// <summary>内容 API：课程树、课时内容与内容库（对话/短文/例句）；登录用户可读，内容定义不含用户进度。</summary>
[ApiController]
[Authorize]
[Route("api/content")]
public sealed class ContentController(ICourseCatalogQuery catalog, IContentLibraryQuery library) : ControllerBase
{
    public const string CoursesPath = "/api/content/courses";
    public const string LessonPath = "/api/content/lessons/{lessonId}";
    public const string DialoguesPath = "/api/content/dialogues";
    public const string EssaysPath = "/api/content/essays";
    public const string SentencesPath = "/api/content/sentences";

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

    [HttpGet("dialogues")]
    public async Task<ActionResult<IReadOnlyList<DialogueDto>>> GetDialogues(
        [FromQuery] string language, CancellationToken cancellationToken)
        => Ok(await library.GetDialoguesAsync(language, cancellationToken));

    [HttpGet("essays")]
    public async Task<ActionResult<IReadOnlyList<EssayDto>>> GetEssays(
        [FromQuery] string language, CancellationToken cancellationToken)
        => Ok(await library.GetEssaysAsync(language, cancellationToken));

    [HttpGet("sentences")]
    public async Task<ActionResult<IReadOnlyList<LibrarySentenceDto>>> GetSentences(
        [FromQuery] string language, [FromQuery] string? conceptKey, CancellationToken cancellationToken)
        => Ok(await library.GetSentencesAsync(language, conceptKey, cancellationToken));
}
