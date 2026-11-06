using BeafSoup.SharedKernel;
using FluentAssertions;
using Xunit;

namespace BeafSoup.SharedKernel.Tests;

public class LanguageCodeTests
{
    [Fact]
    public void Of_BCP47大小写规范化()
    {
        LanguageCode.Of("DE-de").Value.Should().Be("de-DE");
        LanguageCode.Of("zh_cn").Value.Should().Be("zh-CN");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("d")]
    [InlineData("deutsch")]
    [InlineData("de-")]
    [InlineData("-de")]
    [InlineData("de-toolongsegment")]
    public void Of_非法标签_抛领域异常(string value)
    {
        var act = () => LanguageCode.Of(value);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(SharedKernelErrorCodes.InvalidLanguageCode);
    }

    [Fact]
    public void 相等性按值比较()
    {
        LanguageCode.Of("de-DE").Should().Be(LanguageCode.Of("de-DE"));
        LanguageCode.Of("de-DE").Should().Be(LanguageCode.Of("DE_de"));
    }
}

public class SemanticKeyTests
{
    [Theory]
    [InlineData("word.coffee")]
    [InlineData("sentence.greeting.001")]
    [InlineData("ui.button.check")]
    public void Of_合法键(string value)
    {
        SemanticKey.Of(value).Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Word.Coffee")]
    [InlineData(".word")]
    [InlineData("word.")]
    [InlineData("word..coffee")]
    [InlineData("word coffee")]
    [InlineData("-word")]
    public void Of_非法键_抛领域异常(string value)
    {
        var act = () => SemanticKey.Of(value);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(SharedKernelErrorCodes.InvalidSemanticKey);
    }
}

public class VoiceRoleTests
{
    [Fact]
    public void 恰好三个角色()
    {
        Enum.GetValues<VoiceRole>().Should().BeEquivalentTo([VoiceRole.Male, VoiceRole.Female, VoiceRole.Child]);
    }
}
