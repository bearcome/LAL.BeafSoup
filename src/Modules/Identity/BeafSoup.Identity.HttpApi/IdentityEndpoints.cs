using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace BeafSoup.Identity;

/// <summary>身份 API 端点：注册 / 登录 / 退出（心跳端点在 UsageAnalytics.HttpApi，会话统计经 Contracts 组合）。</summary>
public static class IdentityEndpoints
{
    public const string RegisterPath = "/api/identity/register";
    public const string LoginPath = "/api/identity/login";
    public const string LogoutPath = "/api/identity/logout";

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(RegisterPath, async (
            RegisterAccountRequest request,
            IIdentityAppService identityService,
            CancellationToken cancellationToken) =>
        {
            var tokens = await identityService.RegisterAsync(request, cancellationToken);
            return Results.Ok(tokens);
        }).AllowAnonymous();

        app.MapPost(LoginPath, async (
            LoginRequest request,
            IIdentityAppService identityService,
            CancellationToken cancellationToken) =>
        {
            var tokens = await identityService.LoginAsync(request, cancellationToken);
            return Results.Ok(tokens);
        }).AllowAnonymous();

        app.MapPost(LogoutPath, async (
            HttpContext httpContext,
            ISessionAppService sessionService,
            CancellationToken cancellationToken) =>
        {
            var sessionId = AuthenticatedPrincipal.RequireSessionId(httpContext.User);
            await sessionService.EndSessionAsync(sessionId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}
