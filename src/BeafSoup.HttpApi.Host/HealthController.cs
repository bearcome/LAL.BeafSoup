using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.HttpApi.Host;

/// <summary>宿主健康检查接口。</summary>
[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("/health")]
    [AllowAnonymous]
    public ActionResult<object> Get() => Ok(new { status = "ok" });
}
