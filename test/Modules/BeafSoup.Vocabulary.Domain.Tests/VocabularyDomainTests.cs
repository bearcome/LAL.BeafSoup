using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Vocabulary.Domain.Tests;

public class LemmaTests
{
    private static readonly LanguageCode De = LanguageCode.Of("de-DE");
    private static readonly SemanticKey Key = SemanticKey.Of("word.coffee");

    private static Lemma CreateLemma()
    {
        var initial = InflectionForm.Create(Key, [new GrammaticalAnalysis(Case: GrammaticalCase.Nominative, Number: GrammaticalNumber.Singular)]);
        return Lemma.Create(Guid.NewGuid(), De, Key, PartOfSpeech.Noun, GermanArticle.Der, CefrLevel.A1, initial);
    }

    [Fact]
    public void 原形本身必须是InflectionForm()
    {
        var initial = InflectionForm.Create(Key, [new GrammaticalAnalysis()]);
        var lemma = Lemma.Create(Guid.NewGuid(), De, Key, PartOfSpeech.Noun, GermanArticle.Der, CefrLevel.A1, initial);
        lemma.Forms.Should().ContainSingle().Which.SurfaceKey.Should().Be(Key);
    }

    [Fact]
    public void 初始词形语义键必须等于词元键()
    {
        var wrongForm = InflectionForm.Create(SemanticKey.Of("word.coffee.form.gen-sg"), [new GrammaticalAnalysis()]);
        var act = () => Lemma.Create(Guid.NewGuid(), De, Key, PartOfSpeech.Noun, GermanArticle.Der, CefrLevel.A1, wrongForm);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.InitialFormMustMatchLemmaKey);
    }

    [Fact]
    public void 同一表单不允许重复相同语法分析()
    {
        var lemma = CreateLemma();
        var analyses = new[]
        {
            new GrammaticalAnalysis(Case: GrammaticalCase.Accusative, Number: GrammaticalNumber.Singular),
            new GrammaticalAnalysis(Case: GrammaticalCase.Accusative, Number: GrammaticalNumber.Singular),
        };
        var act = () => lemma.AddForm(SemanticKey.Of("word.coffee.form.acc-sg"), analyses);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.DuplicateAnalysis);
    }

    [Fact]
    public void 词形必须至少携带一个语法分析()
    {
        var lemma = CreateLemma();
        var act = () => lemma.AddForm(SemanticKey.Of("word.coffee.form.empty"), []);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.FormRequiresAnalysis);
    }

    [Fact]
    public void 受控标签非空且不重复()
    {
        var lemma = CreateLemma();
        lemma.AddTag("food");
        lemma.Tags.Should().Equal("food");
        var act = () => lemma.AddTag("food");
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.DuplicateTag);
    }
}

public class InflectionResolverTests
{
    [Fact]
    public void 检索键非唯一_返回全部候选()
    {
        var candidates = new[]
        {
            new InflectionCandidate(LemmaId: Guid.NewGuid(), FormId: Guid.NewGuid(), NormalizedSurface: "sie"),
            new InflectionCandidate(LemmaId: Guid.NewGuid(), FormId: Guid.NewGuid(), NormalizedSurface: "sie"),
            new InflectionCandidate(LemmaId: Guid.NewGuid(), FormId: Guid.NewGuid(), NormalizedSurface: "sie"),
        };
        var resolver = new InflectionResolver();
        var matches = resolver.Resolve(candidates, "sie");
        matches.Should().HaveCount(3); // 歧义是常态：禁止凭表层字符串强制唯一归属
    }

    [Fact]
    public void 无候选时返回空列表()
    {
        var resolver = new InflectionResolver();
        resolver.Resolve([new InflectionCandidate(Guid.NewGuid(), Guid.NewGuid(), "kaffee")], "tee").Should().BeEmpty();
    }

    [Fact]
    public void 德语规范化_小写并去首尾空白()
    {
        var normalizer = new GermanSurfaceFormNormalizer();
        normalizer.Language.Should().Be(LanguageCode.Of("de-DE"));
        normalizer.Normalize("  Kaffee ").Should().Be("kaffee");
        normalizer.Normalize("groß").Should().Be("groß"); // 变音符号原样保留
    }
}
