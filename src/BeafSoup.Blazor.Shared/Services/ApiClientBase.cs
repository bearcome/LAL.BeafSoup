using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BeafSoup.Blazor.Shared.Services;

/// <summary>ApiClient 基类：附带令牌、统一连通性上报；实现只调用 Contracts 接口定义的服务端契约。</summary>
public abstract class ApiClientBase
{
    private readonly HttpClient _http;
    private readonly IAuthTokenStore _tokens;
    private readonly IConnectivityState _connectivity;

    protected ApiClientBase(HttpClient http, IAuthTokenStore tokens, IConnectivityState connectivity)
        => (_http, _tokens, _connectivity) = (http, tokens, connectivity);

    protected async Task<TResponse> PostAsync<TRequest, TResponse>(string uri, TRequest body, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, uri, cancellationToken);
        request.Content = JsonContent.Create(body);
        return await SendAsync<TResponse>(request, cancellationToken);
    }

    protected async Task<TResponse?> GetAsync<TResponse>(string uri, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, uri, cancellationToken);
        var response = await SendCoreAsync(request, cancellationToken);
        // 404 视为资源不存在（返回 null），其余非成功统一抛 HttpRequestException，由调用侧按场景处理
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }

    protected async Task PostNoContentAsync(string uri, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, uri, cancellationToken);
        await SendAsync<object>(request, cancellationToken, expectBody: false);
    }

    /// <summary>带令牌认证的通用发送（PATCH 等非 Get/Post 动词使用）；连通性上报与基类一致。</summary>
    protected Task<HttpResponseMessage> SendWithAuthAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => SendCoreAsync(request, cancellationToken);

    private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            _connectivity.ReportUnavailable();
            throw;
        }
        _connectivity.ReportAvailable();
        return response;
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string uri, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, uri);
        var tokens = _tokens.Tokens;
        if (tokens is not null && !_tokens.IsExpired)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return request;
    }

    private async Task<TResponse> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken, bool expectBody = true)
    {
        var response = await SendCoreAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (!expectBody)
        {
            response.Content.Dispose();
            return default!;
        }
        var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidDataException($"服务端响应缺少 {typeof(TResponse).Name} 主体。");
    }
}
