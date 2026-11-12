namespace BeafSoup.Blazor.Shared.Services;

/// <summary>多媒体地址解析：数据层只保存资产相对路径，客户端统一套用可配置基址。</summary>
public static class MediaUrlResolver
{
    public static string? Resolve(string? assetUrl, string? mediaBaseUrl, string applicationBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(assetUrl))
            return null;
        if (Uri.TryCreate(assetUrl, UriKind.Absolute, out var absoluteAsset))
            return absoluteAsset.AbsoluteUri;

        var applicationBase = new Uri(applicationBaseUrl, UriKind.Absolute);
        var configuredBase = string.IsNullOrWhiteSpace(mediaBaseUrl) ? "media/audio/" : mediaBaseUrl.Trim();
        if (!configuredBase.EndsWith('/'))
            configuredBase += "/";

        // 相对基址跟随应用部署子路径；以 / 开头或绝对 URI 的基址保持其显式语义
        var mediaBase = new Uri(applicationBase, configuredBase);
        return new Uri(mediaBase, assetUrl.TrimStart('/')).AbsoluteUri;
    }
}
