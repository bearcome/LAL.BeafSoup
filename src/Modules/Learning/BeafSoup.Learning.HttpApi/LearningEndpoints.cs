using BeafSoup.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace BeafSoup.Learning;

/// <summary>学习 API 端点：全部要求认证；资源归属从验证过的身份派生，客户端伪造 UserId 无效。</summary>
public static class LearningEndpoints
{
    public const string ProfilesPath = "/api/learning/profiles";

    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ProfilesPath, async (
            HttpContext httpContext,
            ILearningProfileAppService profiles,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthenticatedPrincipal.RequireUserId(httpContext.User);
            return Results.Ok(await profiles.GetMineAsync(userId, cancellationToken));
        }).RequireAuthorization();

        app.MapPost(ProfilesPath, async (
            HttpContext httpContext,
            CreateLearnerProfileRequest request,
            ILearningProfileAppService profiles,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthenticatedPrincipal.RequireUserId(httpContext.User);
            var created = await profiles.CreateAsync(userId, request, cancellationToken);
            return Results.Created($"{ProfilesPath}/{created.Id}", created);
        }).RequireAuthorization();

        app.MapPatch($"{ProfilesPath}/{{profileId}}", async (
            HttpContext httpContext,
            Guid profileId,
            UpdateProfilePreferencesRequest request,
            ILearningProfileAppService profiles,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthenticatedPrincipal.RequireUserId(httpContext.User);
            if (!await OwnsProfileAsync(userId, profileId, profiles, cancellationToken))
                return Results.NotFound(); // 不泄漏他人资源是否存在
            return Results.Ok(await profiles.UpdatePreferencesAsync(profileId, request, cancellationToken));
        }).RequireAuthorization();

        app.MapGet($"{ProfilesPath}/{{profileId}}/progress", async (
            HttpContext httpContext,
            Guid profileId,
            ILearningProfileAppService profiles,
            ILearningProgressQuery progress,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthenticatedPrincipal.RequireUserId(httpContext.User);
            if (!await OwnsProfileAsync(userId, profileId, profiles, cancellationToken))
                return Results.NotFound();
            return Results.Ok(await progress.GetProgressAsync(profileId, cancellationToken));
        }).RequireAuthorization();

        app.MapPost($"{ProfilesPath}/{{profileId}}/activities", async (
            HttpContext httpContext,
            Guid profileId,
            SubmitActivityRequest request,
            ILearningProfileAppService profiles,
            ILearningActivityAppService activities,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthenticatedPrincipal.RequireUserId(httpContext.User);
            if (!await OwnsProfileAsync(userId, profileId, profiles, cancellationToken))
                return Results.NotFound();
            return Results.Ok(await activities.SubmitAsync(profileId, request, cancellationToken));
        }).RequireAuthorization();

        app.MapGet($"{ProfilesPath}/{{profileId}}/reviews/due", async (
            HttpContext httpContext,
            Guid profileId,
            int? limit,
            ILearningProfileAppService profiles,
            IReviewAppService reviews,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthenticatedPrincipal.RequireUserId(httpContext.User);
            if (!await OwnsProfileAsync(userId, profileId, profiles, cancellationToken))
                return Results.NotFound();
            return Results.Ok(await reviews.GetDueQueueAsync(profileId, limit ?? 20, cancellationToken));
        }).RequireAuthorization();

        app.MapPost($"{ProfilesPath}/{{profileId}}/reviews/enqueue", async (
            HttpContext httpContext,
            Guid profileId,
            EnqueueNewCardsRequest request,
            ILearningProfileAppService profiles,
            IReviewAppService reviews,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthenticatedPrincipal.RequireUserId(httpContext.User);
            if (!await OwnsProfileAsync(userId, profileId, profiles, cancellationToken))
                return Results.NotFound();
            var inserted = await reviews.EnqueueNewCardsAsync(profileId, request.ConceptIds, cancellationToken);
            return Results.Ok(new EnqueuedNewCardsResponse(inserted));
        }).RequireAuthorization();

        app.MapPost($"{ProfilesPath}/{{profileId}}/reviews/rate", async (
            HttpContext httpContext,
            Guid profileId,
            RateReviewRequest request,
            ILearningProfileAppService profiles,
            IReviewAppService reviews,
            CancellationToken cancellationToken) =>
        {
            var userId = AuthenticatedPrincipal.RequireUserId(httpContext.User);
            if (!await OwnsProfileAsync(userId, profileId, profiles, cancellationToken))
                return Results.NotFound();
            return Results.Ok(await reviews.RateAsync(profileId, request, cancellationToken));
        }).RequireAuthorization();

        return app;
    }

    private static async Task<bool> OwnsProfileAsync(
        Guid userId, Guid profileId, ILearningProfileAppService profiles, CancellationToken cancellationToken)
        => (await profiles.GetMineAsync(userId, cancellationToken)).Any(p => p.Id == profileId);
}
