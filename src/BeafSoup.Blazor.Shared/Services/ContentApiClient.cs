using BeafSoup.Content;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>内容 ApiClient：课程目录（课程树 / 课时内容引用）。</summary>
public sealed class ContentApiClient : ApiClientBase, ICourseCatalogQuery
{
    public ContentApiClient(HttpClient http, IAuthTokenStore tokens, IConnectivityState connectivity)
        : base(http, tokens, connectivity)
    {
    }

    public async Task<IReadOnlyList<CourseTreeDto>> GetCourseTreeAsync(CancellationToken cancellationToken)
        => (await GetAsync<List<CourseTreeDto>>("/api/content/courses", cancellationToken)) ?? [];

    public Task<LessonContentDto?> GetLessonContentAsync(string lessonId, CancellationToken cancellationToken)
        => GetAsync<LessonContentDto>($"/api/content/lessons/{Uri.EscapeDataString(lessonId)}", cancellationToken);
}
