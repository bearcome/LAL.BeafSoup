using BeafSoup.Audio;
using BeafSoup.SharedKernel;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Audio.Domain.Tests;

public class AudioPathBuilderTests
{
    [Fact]
    public void 名词单词音频_含冠词()
    {
        var asset = AudioAsset.Word(LanguageCode.Of("de-DE"), "der", "Kaffee", VoiceRole.Male);
        AudioPathBuilder.BuildRelativePath(asset, "Max").Should().Be("de-DE/der-Kaffee-Max.mp3");
    }

    [Fact]
    public void 无冠词单词音频_省略冠词段()
    {
        var asset = AudioAsset.Word(LanguageCode.Of("de-DE"), article: null, "trinken", VoiceRole.Female);
        AudioPathBuilder.BuildRelativePath(asset, "Mia").Should().Be("de-DE/trinken-Mia.mp3");
    }

    [Fact]
    public void 词内空格转连字符_变音符号保留()
    {
        var asset = AudioAsset.Word(LanguageCode.Of("de-DE"), null, "gute Nacht", VoiceRole.Child);
        AudioPathBuilder.BuildRelativePath(asset, "Lars").Should().Be("de-DE/gute-Nacht-Lars.mp3");

        var umlaut = AudioAsset.Word(LanguageCode.Of("de-DE"), null, "groß", VoiceRole.Male);
        AudioPathBuilder.BuildRelativePath(umlaut, "Max").Should().Be("de-DE/groß-Max.mp3");
    }

    [Fact]
    public void 句子音频()
    {
        var asset = AudioAsset.Sentence(LanguageCode.Of("de-DE"), "s-004217", VoiceRole.Male);
        AudioPathBuilder.BuildRelativePath(asset, "Max").Should().Be("de-DE/s-004217-Max.mp3");
    }

    [Theory]
    [InlineData("Der")]
    [InlineData("die ")]
    [InlineData("ein")]
    public void 非法冠词_抛异常(string article)
    {
        var act = () => AudioAsset.Word(LanguageCode.Of("de-DE"), article, "Kaffee", VoiceRole.Male);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(AudioErrorCodes.InvalidArticle);
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("..")]
    public void 非法文件名片段_抛异常(string token)
    {
        var asset = AudioAsset.Word(LanguageCode.Of("de-DE"), "der", "Kaffee", VoiceRole.Male);
        var act = () => AudioPathBuilder.BuildRelativePath(asset, token);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(AudioErrorCodes.InvalidAssetFileToken);
    }

    [Fact]
    public void 句子id含非法字符_抛异常()
    {
        var act = () => AudioAsset.Sentence(LanguageCode.Of("de-DE"), "s_0001", VoiceRole.Male);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(AudioErrorCodes.InvalidSentenceId);
    }
}
