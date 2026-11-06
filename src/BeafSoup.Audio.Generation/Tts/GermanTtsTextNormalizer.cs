using System.Text;

namespace BeafSoup.Audio.Generation.Tts;

/// <summary>德语 TTS 文本规范化：数字展开为德语数词、压缩空白；生成期完成，应用端不处理。</summary>
public sealed class GermanTtsTextNormalizer : ITtsTextNormalizer
{
    private static readonly string[] DigitWords = ["null", "eins", "zwei", "drei", "vier", "fünf", "sechs", "sieben", "acht", "neun"];

    public string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length + 16);
        var pendingSpace = false;
        foreach (var c in text.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = true;
                continue;
            }
            if (pendingSpace && builder.Length > 0)
                builder.Append(' ');
            pendingSpace = false;
            builder.Append(c is >= '0' and <= '9' ? DigitWords[c - '0'] : c);
        }
        return builder.ToString();
    }
}
