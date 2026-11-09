using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using BeafSoup.SharedKernel;

namespace BeafSoup.Localization;

/// <summary>本地化读模型端点：界面文案目录（公开只读；不回写）。</summary>
public static class LocalizationEndpoints
{
    public const string UiTextsPath = "/api/localization/ui-texts";

    public static IEndpointRouteBuilder MapLocalizationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(UiTextsPath, async (
            string domain,
            IUiTextCatalogQuery catalogQuery,
            CancellationToken cancellationToken) =>
        {
            var catalog = await catalogQuery.GetCatalogAsync(domain, cancellationToken);
            return catalog.TextsByKeyAndLanguage.Count > 0 ? Results.Ok(catalog) : Results.NotFound();
        }).AllowAnonymous();

        return app;
    }
}
