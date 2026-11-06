namespace BeafSoup.Audio.Generation.Tts;

/// <summary>生成期 TTS 文本规范化（数字展开/空白规整），应用端原样播放。</summary>
public interface ITtsTextNormalizer
{
    string Normalize(string text);
}
