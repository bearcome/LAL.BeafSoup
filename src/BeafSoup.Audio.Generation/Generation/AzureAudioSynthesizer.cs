using System.Text;

namespace BeafSoup.Audio.Generation.Generation;

/// <summary>Azure TTS 合成器（Speech REST API v1）；密钥只来自 secret.json 运行时绑定，绝不打印。</summary>
public sealed class AzureAudioSynthesizer : IAudioSynthesizer
{
    private const string OutputFormat = "audio-24khz-48kbitrate-mono-mp3";

    private readonly HttpClient _httpClient;
    private readonly string[] _requestUris;

    public AzureAudioSynthesizer(AzureTtsOptions options)
    {
        var regional = $"https://{options.Region}.tts.speech.microsoft.com/cognitiveservices/v1";
        _requestUris = !string.IsNullOrEmpty(options.Endpoint) && !options.Endpoint.Contains(".tts.speech.microsoft.com")
            ? [options.Endpoint.TrimEnd('/') + "/cognitiveservices/v1", regional]
            : [string.IsNullOrEmpty(options.Endpoint) ? regional : options.Endpoint.TrimEnd('/')];
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", options.ApiKey);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Microsoft-OutputFormat", OutputFormat);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "BeafSoup.Audio.Generation");
    }

    public async Task SynthesizeToFileAsync(string ssml, string outputPath, CancellationToken cancellationToken)
    {
        using var content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml");
        HttpResponseMessage? response = null;
        var attempts = new List<string>();
        foreach (var uri in _requestUris)
        {
            response = await _httpClient.PostAsync(uri, content, cancellationToken);
            if (response.IsSuccessStatusCode)
                break;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            attempts.Add($"{uri} → {(int)response.StatusCode}：{body}");
            if ((int)response.StatusCode is < 500 and not 404)
                break; // 401/403 等换端点无意义
            response.Dispose();
            response = null;
        }

        if (response is null || !response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Azure TTS 合成失败：{string.Join("；", attempts)}。请检查 secret.json 密钥/区域与种子中的语音名。");

        var directory = Path.GetDirectoryName(outputPath)!;
        Directory.CreateDirectory(directory);
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(fileStream, cancellationToken);
        response.Dispose();
    }
}
