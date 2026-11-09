using BeafSoup.Localization;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>界面文案 ApiClient：按 domain 拉取全部 (键, 语言) 文本目录。</summary>
public sealed class UiTextApiClient : ApiClientBase, IUiTextCatalogQuery
{
    public UiTextApiClient(HttpClient http, IAuthTokenStore tokens, IConnectivityState connectivity)
        : base(http, tokens, connectivity)
    {
    }

    public async Task<UiTextCatalogDto> GetCatalogAsync(string domain, CancellationToken cancellationToken)
        => await GetAsync<UiTextCatalogDto>($"/api/localization/ui-texts?domain={Uri.EscapeDataString(domain)}", cancellationToken)
           ?? throw new InvalidDataException($"文案目录 domain={domain} 不可用。");
}
