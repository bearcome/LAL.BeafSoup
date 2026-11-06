using BeafSoup.Audio.Generation.Tts;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Audio.Generation.Tests;

public class GermanTtsTextNormalizerTests
{
    [Fact]
    public void 数字展开为德语数词()
    {
        var normalizer = new GermanTtsTextNormalizer();
        normalizer.Normalize("Ich habe 7 Äpfel.").Should().Be("Ich habe sieben Äpfel.");
    }

    [Fact]
    public void 多余空白被压缩()
    {
        var normalizer = new GermanTtsTextNormalizer();
        normalizer.Normalize("  Guten   Tag!  ").Should().Be("Guten Tag!");
    }
}

public class SsmlBuilderTests
{
    [Fact]
    public void SSML包含语音与转义正文()
    {
        var ssml = new SsmlBuilder().Build("de-DE-ConradNeural", "Tom & Jerry <sind> \"toll\"");
        ssml.Should().Contain("<voice name=\"de-DE-ConradNeural\">");
        ssml.Should().Contain("Tom &amp; Jerry &lt;sind&gt; &quot;toll&quot;");
        ssml.Should().NotContain("<sind>");
    }

    [Fact]
    public void 可选语速与音高()
    {
        var ssml = new SsmlBuilder().Build("de-DE-KasperNeural", "Hallo", rate: "-10%", pitch: "+2st");
        ssml.Should().Contain("rate=\"-10%\"");
        ssml.Should().Contain("pitch=\"+2st\"");
    }
}

public class StableIdTests
{
    [Fact]
    public void 同输入同Id_不同输入不同Id()
    {
        StableId.For("word.coffee").Should().Be(StableId.For("word.coffee"));
        StableId.For("word.coffee").Should().NotBe(StableId.For("word.tea"));
    }
}
