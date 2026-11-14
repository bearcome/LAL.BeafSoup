using BeafSoup.Blazor.Shared.Services;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Blazor.Tests;

public sealed class SentenceSegmenterTests
{
    [Fact]
    public void 最长短语匹配_整体消费且不重复输出后半部分()
    {
        var segments = SentenceSegmenter.Build(
            "Guten Tag!", blankStart: 0, blankLength: 0,
            phrase => phrase == "Guten Tag");

        segments.Select(s => s.Text).Should().Equal("Guten Tag", "!");
        segments.Should().ContainSingle(s => s.HasLink).Which.Text.Should().Be("Guten Tag");
    }

    [Fact]
    public void 未命中词典的词保持普通文本_命中词可点击()
    {
        var segments = SentenceSegmenter.Build(
            "Ich trinke Kaffee.", blankStart: 0, blankLength: 0,
            word => word is "trinke" or "Kaffee");

        segments.Where(s => s.HasLink).Select(s => s.Text).Should().Equal("trinke", "Kaffee");
        string.Concat(segments.Select(s => s.Text)).Should().Be("Ich trinke Kaffee.");
    }

    [Fact]
    public void 空位渲染为下划线占位且不参与短语匹配()
    {
        const string text = "Ich trinke Kaffee.";
        var segments = SentenceSegmenter.Build(
            text, blankStart: 4, blankLength: 6,
            word => word == "trinke");

        segments.Should().ContainSingle(s => s.IsBlank).Which.Text.Should().Be("______");
        segments.Should().NotContain(s => s.HasLink && s.Text == "trinke");
        string.Concat(segments.Select(s => s.IsBlank ? "______" : s.Text))
            .Should().Be("Ich ______ Kaffee.");
    }

    [Fact]
    public void 短语匹配不跨越两个空格()
    {
        var segments = SentenceSegmenter.Build(
            "Guten  Tag!", blankStart: 0, blankLength: 0,
            phrase => phrase.StartsWith("Guten"));

        segments.Should().NotContain(s => s.HasLink && s.Text == "Guten  Tag");
        segments.Where(s => s.HasLink).Select(s => s.Text).Should().Equal("Guten");
    }
}
