using BeafSoup.Localization;
using BeafSoup.SharedKernel;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Localization.Domain.Tests;

public class LocalizedTextTests
{
    private static readonly SemanticKey Key = SemanticKey.Of("sentence.greeting.001");
    private static readonly LanguageCode De = LanguageCode.Of("de-DE");

    [Fact]
    public void 创建时修订号为1()
    {
        LocalizedText.Create(Key, De, "Guten Tag!").Revision.Should().Be(1);
    }

    [Fact]
    public void 文本变更必须递增修订号()
    {
        var text = LocalizedText.Create(Key, De, "Guten Tag!");
        var revised = text.Revise("Guten Morgen!");
        revised.Revision.Should().Be(2);
        revised.Text.Should().Be("Guten Morgen!");
    }

    [Fact]
    public void 同值重写不递增修订号()
    {
        var text = LocalizedText.Create(Key, De, "Guten Tag!");
        text.Revise("Guten Tag!").Revision.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void 空文本_抛异常(string value)
    {
        var act = () => LocalizedText.Create(Key, De, value);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(LocalizationErrorCodes.EmptyText);
    }

    [Fact]
    public void 修订失配即标注失效()
    {
        var text = LocalizedText.Create(Key, De, "Guten Tag!");
        text.IsStaleFor(2).Should().BeTrue();
        text.IsStaleFor(1).Should().BeFalse();
    }
}

public class VoiceRoleConfigTests
{
    private static readonly LanguageCode De = LanguageCode.Of("de-DE");

    [Fact]
    public void 完整集合恰好三角色()
    {
        var configs = new[]
        {
            VoiceRoleConfig.Create(De, VoiceRole.Male, "Max", "de-DE-ConradNeural"),
            VoiceRoleConfig.Create(De, VoiceRole.Female, "Mia", "de-DE-KatjaNeural"),
            VoiceRoleConfig.Create(De, VoiceRole.Child, "Lars", "de-DE-KasperNeural"),
        };
        var complete = VoiceRoleConfig.EnsureCompleteSet(De, configs);
        complete.Should().HaveCount(3);
    }

    [Fact]
    public void 缺角色或重复角色_抛异常()
    {
        var onlyTwo = new[]
        {
            VoiceRoleConfig.Create(De, VoiceRole.Male, "Max", "de-DE-ConradNeural"),
            VoiceRoleConfig.Create(De, VoiceRole.Female, "Mia", "de-DE-KatjaNeural"),
        };
        var act1 = () => VoiceRoleConfig.EnsureCompleteSet(De, onlyTwo);
        act1.Should().Throw<DomainException>().Which.Code.Should().Be(LocalizationErrorCodes.MissingVoiceRole);

        var duplicated = new[]
        {
            VoiceRoleConfig.Create(De, VoiceRole.Male, "Max", "de-DE-ConradNeural"),
            VoiceRoleConfig.Create(De, VoiceRole.Male, "Max2", "de-DE-ConradNeural"),
            VoiceRoleConfig.Create(De, VoiceRole.Female, "Mia", "de-DE-KatjaNeural"),
        };
        var act2 = () => VoiceRoleConfig.EnsureCompleteSet(De, duplicated);
        act2.Should().Throw<DomainException>().Which.Code.Should().Be(LocalizationErrorCodes.DuplicateVoiceRole);
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("..")]
    [InlineData("-Max")]
    [InlineData("")]
    public void 非法文件名片段_抛异常(string token)
    {
        var act = () => VoiceRoleConfig.Create(De, VoiceRole.Male, token, "de-DE-ConradNeural");
        act.Should().Throw<DomainException>().Which.Code.Should().Be(LocalizationErrorCodes.InvalidAssetFileToken);
    }

    [Fact]
    public void 语言不一致_抛异常()
    {
        var configs = new[]
        {
            VoiceRoleConfig.Create(De, VoiceRole.Male, "Max", "de-DE-ConradNeural"),
            VoiceRoleConfig.Create(LanguageCode.Of("en-US"), VoiceRole.Female, "Mia", "en-US-AriaNeural"),
            VoiceRoleConfig.Create(De, VoiceRole.Child, "Lars", "de-DE-KasperNeural"),
        };
        var act = () => VoiceRoleConfig.EnsureCompleteSet(De, configs);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(LocalizationErrorCodes.WrongLanguage);
    }
}
