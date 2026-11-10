using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.Identity;

/// <summary>身份 API：注册 / 登录 / 退出（心跳接口在 UsageAnalytics.HttpApi，会话统计经 Contracts 组合）。</summary>
[ApiController]
[Route("api/identity")]
public sealed class IdentityController(
    IIdentityAppService identityService,
    ISessionAppService sessionService) : ControllerBase
{
    public const string RegisterPath = "/api/identity/register";
    public const string LoginPath = "/api/identity/login";
    public const string LogoutPath = "/api/identity/logout";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensDto>> Register(
        [FromBody] RegisterAccountRequest request,
        CancellationToken cancellationToken)
        => Ok(await identityService.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
        => Ok(await identityService.LoginAsync(request, cancellationToken));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sessionId = AuthenticatedPrincipal.RequireSessionId(User);
        await sessionService.EndSessionAsync(sessionId, cancellationToken);
        return NoContent();
    }
}
