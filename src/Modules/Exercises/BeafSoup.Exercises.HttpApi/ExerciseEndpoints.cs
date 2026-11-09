using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace BeafSoup.Exercises;

/// <summary>习题 API 端点：呈现（校验后下发）与判分（服务端权威 + 幂等活动记录）。</summary>
public static class ExerciseEndpoints
{
    public const string PresentationsPath = "/api/exercises";
    public const string GradePath = "/api/exercises/{instanceId}/grade";

    public static IEndpointRouteBuilder MapExerciseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(PresentationsPath, async (
            [FromQuery] Guid[] ids,
            IExercisePresentationQuery presentations,
            CancellationToken cancellationToken)
            => Results.Ok(await presentations.GetPresentationsAsync(ids, cancellationToken)))
            .RequireAuthorization();

        app.MapPost(GradePath, async (
            Guid instanceId,
            GradeAnswerRequest request,
            IExerciseGradingAppService grading,
            CancellationToken cancellationToken)
            => Results.Ok(await grading.GradeAsync(instanceId, request, cancellationToken)))
            .RequireAuthorization();

        return app;
    }
}
