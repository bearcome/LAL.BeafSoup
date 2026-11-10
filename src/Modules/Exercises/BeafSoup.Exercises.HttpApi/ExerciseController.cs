using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.Exercises;

/// <summary>习题 API：呈现（校验后下发）与判分（服务端权威 + 幂等活动记录）。</summary>
[ApiController]
[Authorize]
[Route("api/exercises")]
public sealed class ExerciseController(
    IExercisePresentationQuery presentations,
    IExerciseGradingAppService grading) : ControllerBase
{
    public const string PresentationsPath = "/api/exercises";
    public const string GradePath = "/api/exercises/{instanceId}/grade";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExercisePresentationDto>>> GetPresentations(
        [FromQuery] Guid[] ids,
        CancellationToken cancellationToken)
        => Ok(await presentations.GetPresentationsAsync(ids, cancellationToken));

    [HttpPost("{instanceId}/grade")]
    public async Task<ActionResult<GradeAnswerResponse>> Grade(
        Guid instanceId,
        [FromBody] GradeAnswerRequest request,
        CancellationToken cancellationToken)
        => Ok(await grading.GradeAsync(instanceId, request, cancellationToken));
}
