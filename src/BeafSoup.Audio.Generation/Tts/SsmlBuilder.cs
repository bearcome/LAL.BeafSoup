using System.Net;
using System.Text;

namespace BeafSoup.Audio.Generation.Tts;

/// <summary>SSML 渲染：voice 选择 Azure 语音，prosody 控制语速/音高；正文先 XML 转义。</summary>
public sealed class SsmlBuilder
{
    public string Build(string voiceName, string text, string? rate = null, string? pitch = null)
    {
        var inner = WebUtility.HtmlEncode(text);
        if (rate is not null || pitch is not null)
        {
            var prosody = new StringBuilder("<prosody");
            if (rate is not null)
                prosody.Append($" rate=\"{rate}\"");
            if (pitch is not null)
                prosody.Append($" pitch=\"{pitch}\"");
            prosody.Append('>').Append(inner).Append("</prosody>");
            inner = prosody.ToString();
        }
        return $"""<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="de-DE"><voice name="{voiceName}">{inner}</voice></speak>""";
    }
}
