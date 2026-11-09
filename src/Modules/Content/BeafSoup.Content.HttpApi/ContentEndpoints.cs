using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace BeafSoup.Content;

/// <summary>内容 API 端点：课程树与课时内容（登录用户可读；内容定义不含用户进度）。</summary>
public static class ContentEndpoints
{
    public const string CoursesPath = "/api/content/courses";
    public const string LessonPath = "/api/content/lessons/{lessonId}";

    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(CoursesPath, async (
            ICourseCatalogQuery catalog,
            CancellationToken cancellationToken) =>
            Results.Ok(await catalog.GetCourseTreeAsync(cancellationToken)))
            .RequireAuthorization();

        app.MapGet(LessonPath, async (
            string lessonId,
            ICourseCatalogQuery catalog,
            CancellationToken cancellationToken)
            => await catalog.GetLessonContentAsync(lessonId, cancellationToken) is { } lesson
                ? Results.Ok(lesson)
                : Results.NotFound())
            .RequireAuthorization();

        return app;
    }
}
