using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeafSoup.Localization;

/// <summary>本地化读模型 API：界面文案目录（公开只读；不回写）。</summary>
[ApiController]
[Route("api/localization")]
public sealed class LocalizationController(IUiTextCatalogQuery catalogQuery) : ControllerBase
{
    public const string UiTextsPath = "/api/localization/ui-texts";

    [HttpGet("ui-texts")]
    [AllowAnonymous]
    public async Task<ActionResult<UiTextCatalogDto>> GetUiTexts(
        [FromQuery] string domain,
        CancellationToken cancellationToken)
    {
        var catalog = await catalogQuery.GetCatalogAsync(domain, cancellationToken);
        return catalog.TextsByKeyAndLanguage.Count > 0 ? Ok(catalog) : NotFound();
    }
}
