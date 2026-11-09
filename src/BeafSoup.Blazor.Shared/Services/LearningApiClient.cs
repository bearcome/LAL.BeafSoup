using BeafSoup.Learning;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>学习模块 ApiClient：实现 Learning Contracts 的客户端侧；userId 取自本地缓存令牌（最终以服务端身份为准）。</summary>
public sealed class LearningApiClient : ApiClientBase,
    ILearningProfileAppService, ILearningProgressQuery, IReviewAppService, ILearningActivityAppService
{
    public LearningApiClient(HttpClient http, IAuthTokenStore tokens, IConnectivityState connectivity)
        : base(http, tokens, connectivity)
    {
    }

    public async Task<IReadOnlyList<LearnerProfileDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken)
        => (await GetAsync<List<LearnerProfileDto>>("/api/learning/profiles", cancellationToken)) ?? [];

    public Task<LearnerProfileDto> CreateAsync(Guid userId, CreateLearnerProfileRequest request, CancellationToken cancellationToken)
        => PostAsync<CreateLearnerProfileRequest, LearnerProfileDto>("/api/learning/profiles", request, cancellationToken);

    public Task<LearnerProfileDto> UpdatePreferencesAsync(Guid profileId, UpdateProfilePreferencesRequest request, CancellationToken cancellationToken)
        => SendAsync<UpdateProfilePreferencesRequest, LearnerProfileDto>(
            HttpMethod.Patch, $"/api/learning/profiles/{profileId}", request, cancellationToken);

    public Task<SubmitActivityResponse> SubmitAsync(Guid learnerProfileId, SubmitActivityRequest request, CancellationToken cancellationToken)
        => PostAsync<SubmitActivityRequest, SubmitActivityResponse>(
            $"/api/learning/profiles/{learnerProfileId}/activities", request, cancellationToken);

    public async Task<IReadOnlyList<ReviewQueueItemDto>> GetDueQueueAsync(Guid learnerProfileId, int limit, CancellationToken cancellationToken)
        => (await GetAsync<List<ReviewQueueItemDto>>(
            $"/api/learning/profiles/{learnerProfileId}/reviews/due?limit={limit}", cancellationToken)) ?? [];

    public async Task<int> EnqueueNewCardsAsync(Guid learnerProfileId, IReadOnlyList<Guid> candidateConceptIds, CancellationToken cancellationToken)
        => (await PostAsync<EnqueueNewCardsRequest, EnqueuedNewCardsResponse>(
            $"/api/learning/profiles/{learnerProfileId}/reviews/enqueue",
            new EnqueueNewCardsRequest(candidateConceptIds), cancellationToken)).Inserted;

    public Task<ProgressSnapshotDto> RateAsync(Guid learnerProfileId, RateReviewRequest request, CancellationToken cancellationToken)
        => PostAsync<RateReviewRequest, ProgressSnapshotDto>(
            $"/api/learning/profiles/{learnerProfileId}/reviews/rate", request, cancellationToken);

    public async Task<ProgressSnapshotDto> GetProgressAsync(Guid learnerProfileId, CancellationToken cancellationToken)
        => await GetAsync<ProgressSnapshotDto>($"/api/learning/profiles/{learnerProfileId}/progress", cancellationToken)
           ?? throw new InvalidDataException("进度快照不可用。");

    private async Task<TResponse> SendAsync<TRequest, TResponse>(HttpMethod method, string uri, TRequest body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri)
        {
            Content = System.Net.Http.Json.JsonContent.Create(body),
        };
        using var response = await SendWithAuthAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken))!;
    }
}
