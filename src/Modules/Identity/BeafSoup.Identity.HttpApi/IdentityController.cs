using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.Identity;

/// <summary>身份 API：注册 / 登录 / 退出 / 邮箱验证 / 密码找回 / 注销与数据导出（心跳接口在 UsageAnalytics.HttpApi）。</summary>
[ApiController]
[Route("api/identity")]
public sealed class IdentityController(
    IIdentityAppService identityService,
    ISessionAppService sessionService,
    IAccountLifecycleAppService lifecycle) : ControllerBase
{
    public const string RegisterPath = "/api/identity/register";
    public const string LoginPath = "/api/identity/login";
    public const string LogoutPath = "/api/identity/logout";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegistrationResultDto>> Register(
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

    /// <summary>申请邮箱验证码：无论账户是否存在都返回已受理（不泄露账户状态）。</summary>
    [HttpPost("email/verification")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestEmailVerification(
        [FromBody] EmailRequest request, CancellationToken cancellationToken)
    {
        await lifecycle.RequestEmailVerificationAsync(request, cancellationToken);
        return Accepted();
    }

    /// <summary>校验验证码：成功返回新会话（未验证账户由此进入已登录状态）。</summary>
    [HttpPost("email/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokensDto>> VerifyEmail(
        [FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
        => Ok(await lifecycle.VerifyEmailAsync(request, cancellationToken));

    /// <summary>申请密码重置码：无论账户是否存在都返回已受理（不泄露账户状态）。</summary>
    [HttpPost("password/forgot")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] EmailRequest request, CancellationToken cancellationToken)
    {
        await lifecycle.RequestPasswordResetAsync(request, cancellationToken);
        return Accepted();
    }

    /// <summary>用重置码设置新密码；成功后全部旧会话失效。</summary>
    [HttpPost("password/reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await lifecycle.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("account")]
    [Authorize]
    public async Task<ActionResult<AccountProfileDto>> GetAccount(CancellationToken cancellationToken)
        => Ok(await lifecycle.GetAccountAsync(AuthenticatedPrincipal.RequireUserId(User), cancellationToken));

    [HttpGet("account/export")]
    [Authorize]
    public async Task<ActionResult<AccountExportDto>> ExportAccount(CancellationToken cancellationToken)
        => Ok(await lifecycle.ExportAsync(AuthenticatedPrincipal.RequireUserId(User), cancellationToken));

    [HttpPost("account/delete")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount(
        [FromBody] DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        await lifecycle.DeleteAccountAsync(AuthenticatedPrincipal.RequireUserId(User), request, cancellationToken);
        return NoContent();
    }
}
