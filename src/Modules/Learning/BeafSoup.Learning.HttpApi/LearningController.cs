using BeafSoup.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.Learning;

/// <summary>学习 API：全部要求认证；资源归属从验证过的身份派生，客户端伪造 UserId 无效。</summary>
[ApiController]
[Authorize]
[Route(ProfilesPath)]
public sealed class LearningController(
    ILearningProfileAppService profiles,
    ILearningActivityAppService activities,
    IReviewAppService reviews,
    ILearningProgressQuery progress) : ControllerBase
{
    public const string ProfilesPath = "/api/learning/profiles";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LearnerProfileDto>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = AuthenticatedPrincipal.RequireUserId(User);
        return Ok(await profiles.GetMineAsync(userId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<LearnerProfileDto>> Create(
        [FromBody] CreateLearnerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = AuthenticatedPrincipal.RequireUserId(User);
        var created = await profiles.CreateAsync(userId, request, cancellationToken);
        return Created($"{ProfilesPath}/{created.Id}", created);
    }

    [HttpPatch("{profileId}")]
    public async Task<ActionResult<LearnerProfileDto>> UpdatePreferences(
        Guid profileId,
        [FromBody] UpdateProfilePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (!await OwnsProfileAsync(profileId, cancellationToken))
            return NotFound(); // 不泄漏他人资源是否存在
        return Ok(await profiles.UpdatePreferencesAsync(profileId, request, cancellationToken));
    }

    [HttpGet("{profileId}/progress")]
    public async Task<ActionResult<ProgressSnapshotDto>> GetProgress(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        if (!await OwnsProfileAsync(profileId, cancellationToken))
            return NotFound();
        return Ok(await progress.GetProgressAsync(profileId, cancellationToken));
    }

    [HttpPost("{profileId}/activities")]
    public async Task<ActionResult<SubmitActivityResponse>> SubmitActivity(
        Guid profileId,
        [FromBody] SubmitActivityRequest request,
        CancellationToken cancellationToken)
    {
        if (!await OwnsProfileAsync(profileId, cancellationToken))
            return NotFound();
        return Ok(await activities.SubmitAsync(profileId, request, cancellationToken));
    }

    [HttpGet("{profileId}/reviews/due")]
    public async Task<ActionResult<IReadOnlyList<ReviewQueueItemDto>>> GetDueQueue(
        Guid profileId,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        if (!await OwnsProfileAsync(profileId, cancellationToken))
            return NotFound();
        return Ok(await reviews.GetDueQueueAsync(profileId, limit ?? 20, cancellationToken));
    }

    [HttpPost("{profileId}/reviews/enqueue")]
    public async Task<ActionResult<EnqueuedNewCardsResponse>> EnqueueNewCards(
        Guid profileId,
        [FromBody] EnqueueNewCardsRequest request,
        CancellationToken cancellationToken)
    {
        if (!await OwnsProfileAsync(profileId, cancellationToken))
            return NotFound();
        var inserted = await reviews.EnqueueNewCardsAsync(profileId, request.ConceptIds, cancellationToken);
        return Ok(new EnqueuedNewCardsResponse(inserted));
    }

    [HttpPost("{profileId}/reviews/rate")]
    public async Task<ActionResult<ProgressSnapshotDto>> RateReview(
        Guid profileId,
        [FromBody] RateReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!await OwnsProfileAsync(profileId, cancellationToken))
            return NotFound();
        return Ok(await reviews.RateAsync(profileId, request, cancellationToken));
    }

    private async Task<bool> OwnsProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var userId = AuthenticatedPrincipal.RequireUserId(User);
        return (await profiles.GetMineAsync(userId, cancellationToken)).Any(p => p.Id == profileId);
    }
}
