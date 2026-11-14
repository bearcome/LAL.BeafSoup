using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.HttpApi.Host;

/// <summary>
/// 开发/测试专用邮件发件箱：仅在 Development / Testing 环境暴露；其他环境返回 404。
/// 用于本地调试与自动化测试读取验证码，不承载任何生产数据。
/// </summary>
[ApiController]
[Route("api/dev/email-outbox")]
public sealed class DevEmailOutboxController(
    IWebHostEnvironment environment,
    IServiceProvider services) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            return NotFound();
        var sender = services.GetService<DevelopmentEmailSender>();
        return sender is null ? NotFound() : Ok(sender.Messages);
    }
}
