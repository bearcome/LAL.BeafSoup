using FluentAssertions;
using Xunit;

namespace BeafSoup.SharedKernel.Tests;

public class TextNormalizationTests
{
    private static readonly ITextNormalizer German = TextNormalizerRegistry.For(LanguageCode.German);
    private static readonly ITextNormalizer Chinese = TextNormalizerRegistry.For(LanguageCode.Chinese);

    [Fact]
    public void Normalize_VariousUmlautCaseAndPunctuation_Folded()
    {
        German.Normalize("  der Über-Fluss! ", ArticleTolerance.Strict)
            .Should().Be("der uberfluss");
        German.Normalize("Straße", ArticleTolerance.Strict)
            .Should().Be("strasse");
        German.Normalize("GÜTEN   TAG", ArticleTolerance.Strict)
            .Should().Be("guten tag");
    }

    [Fact]
    public void Normalize_TolerateLeadingArticle_ArticleStripped()
    {
        German.Normalize("der Kaffee", ArticleTolerance.TolerateLeadingArticle)
            .Should().Be("kaffee");
        // 冠词不在词首（如介词短语内）不受影响
        German.Normalize("für den Kaffee", ArticleTolerance.TolerateLeadingArticle)
            .Should().Be("fur den kaffee");
    }

    [Fact]
    public void Normalize_Strict_ArticleRequired()
    {
        German.Normalize("der Kaffee", ArticleTolerance.Strict)
            .Should().Be("der kaffee");
    }

    [Fact]
    public void Normalize_Chinese_CollapsesWhitespaceOnly()
    {
        Chinese.Normalize("  你好吗   世界 ", ArticleTolerance.Strict)
            .Should().Be("你好吗 世界");
    }

    [Fact]
    public void For_UnsupportedLanguage_Throws()
    {
        var act = () => TextNormalizerRegistry.For(LanguageCode.Of("fr-FR"));
        act.Should().Throw<DomainException>()
            .Where(e => e.Code == SharedKernelErrorCodes.UnsupportedLanguageProcessing);
    }
}
