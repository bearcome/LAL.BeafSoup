using BeafSoup.Blazor.Shared.Services;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Blazor.Tests;

public sealed class MediaUrlResolverTests
{
    [Theory]
    [InlineData("media/audio/", "https://localhost:5280/", "https://localhost:5280/media/audio/de-DE/trinken-Mia.mp3")]
    [InlineData("media/audio", "https://localhost:5280/german/", "https://localhost:5280/german/media/audio/de-DE/trinken-Mia.mp3")]
    [InlineData("/media/audio/", "https://localhost:5280/german/", "https://localhost:5280/media/audio/de-DE/trinken-Mia.mp3")]
    [InlineData("https://cdn.example.test/assets/audio", "https://localhost:5280/", "https://cdn.example.test/assets/audio/de-DE/trinken-Mia.mp3")]
    public void Resolve_combines_configured_base_with_relative_asset(
        string mediaBaseUrl, string applicationBaseUrl, string expected)
    {
        var resolved = MediaUrlResolver.Resolve(
            "de-DE/trinken-Mia.mp3", mediaBaseUrl, applicationBaseUrl);

        resolved.Should().Be(expected);
    }

    [Fact]
    public void Resolve_preserves_absolute_asset_url()
    {
        const string assetUrl = "https://cdn.example.test/de-DE/trinken-Mia.mp3";

        MediaUrlResolver.Resolve(assetUrl, "media/audio/", "https://localhost:5280/")
            .Should().Be(assetUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Resolve_returns_null_for_missing_asset(string? assetUrl)
    {
        MediaUrlResolver.Resolve(assetUrl, "media/audio/", "https://localhost:5280/")
            .Should().BeNull();
    }
}
