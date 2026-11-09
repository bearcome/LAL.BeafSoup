using System.Text.Json;
using BeafSoup.Exercises;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>习题 ApiClient：呈现（校验后下发）与服务端权威判分。</summary>
public sealed class ExerciseApiClient : ApiClientBase, IExercisePresentationQuery, IExerciseGradingAppService
{
    public ExerciseApiClient(HttpClient http, IAuthTokenStore tokens, IConnectivityState connectivity)
        : base(http, tokens, connectivity)
    {
    }

    public async Task<IReadOnlyList<ExercisePresentationDto>> GetPresentationsAsync(
        IReadOnlyList<Guid> instanceIds, CancellationToken cancellationToken)
        => instanceIds.Count == 0
            ? []
            : (await GetAsync<List<ExercisePresentationDto>>(
                $"/api/exercises?ids={string.Join("&ids=", instanceIds.Select(i => i.ToString()))}",
                cancellationToken)) ?? [];

    public Task<GradeAnswerResponse> GradeAsync(
        Guid exerciseInstanceId, GradeAnswerRequest request, CancellationToken cancellationToken)
        => PostAsync<GradeAnswerRequest, GradeAnswerResponse>(
            $"/api/exercises/{exerciseInstanceId}/grade", request, cancellationToken);

    /// <summary>客户端生成稳定活动 ID/去重键：同一次作答的重试（超时重放）由服务端幂等去重。</summary>
    public static Guid NewActivityId() => Guid.NewGuid();

    public static string AnswerJson<T>(T answer) => JsonSerializer.Serialize(answer);
}
