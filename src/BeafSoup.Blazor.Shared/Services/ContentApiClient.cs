using BeafSoup.Content;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>内容 ApiClient：课程目录（课程树 / 课时内容引用）与内容库（对话 / 短文 / 例句）。</summary>
public sealed class ContentApiClient : ApiClientBase, ICourseCatalogQuery, IContentLibraryQuery
{
    public ContentApiClient(HttpClient http, IAuthTokenStore tokens, IConnectivityState connectivity)
        : base(http, tokens, connectivity)
    {
    }

    public async Task<IReadOnlyList<CourseTreeDto>> GetCourseTreeAsync(CancellationToken cancellationToken)
        => (await GetAsync<List<CourseTreeDto>>("/api/content/courses", cancellationToken)) ?? [];

    public Task<LessonContentDto?> GetLessonContentAsync(string lessonId, CancellationToken cancellationToken)
        => GetAsync<LessonContentDto>($"/api/content/lessons/{Uri.EscapeDataString(lessonId)}", cancellationToken);

    public async Task<IReadOnlyList<DialogueDto>> GetDialoguesAsync(string language, CancellationToken cancellationToken)
        => (await GetAsync<List<DialogueDto>>(
            $"/api/content/dialogues?language={Uri.EscapeDataString(language)}", cancellationToken)) ?? [];

    public async Task<IReadOnlyList<EssayDto>> GetEssaysAsync(string language, CancellationToken cancellationToken)
        => (await GetAsync<List<EssayDto>>(
            $"/api/content/essays?language={Uri.EscapeDataString(language)}", cancellationToken)) ?? [];

    public async Task<IReadOnlyList<LibrarySentenceDto>> GetSentencesAsync(
        string language, string? conceptKey, CancellationToken cancellationToken)
    {
        var url = $"/api/content/sentences?language={Uri.EscapeDataString(language)}";
        if (!string.IsNullOrWhiteSpace(conceptKey))
            url += $"&conceptKey={Uri.EscapeDataString(conceptKey)}";
        return (await GetAsync<List<LibrarySentenceDto>>(url, cancellationToken)) ?? [];
    }
}
