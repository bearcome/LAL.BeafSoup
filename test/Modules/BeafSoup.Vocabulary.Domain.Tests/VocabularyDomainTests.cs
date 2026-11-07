using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Vocabulary.Domain.Tests;

public class LexicalConceptTests
{
    private static readonly LanguageCode De = LanguageCode.Of("de-DE");
    private static readonly LanguageCode Zh = LanguageCode.Of("zh-CN");

    private static LexicalConcept CreateDrinkConcept()
    {
        var concept = new LexicalConcept(SemanticKey.Of("concept.drink"), PartOfSpeech.Verb, LexemeKind.Word);
        concept.AddLexeme(De, "trinken", isPrimary: true, LexemeRegister.Neutral, article: null, CefrLevel.A1);
        concept.AddLexeme(De, "zechen", isPrimary: false, LexemeRegister.Elevated, article: null, CefrLevel.B2);
        concept.AddLexeme(Zh, "喝", isPrimary: true, LexemeRegister.Common, article: null, CefrLevel.A1);
        concept.AddLexeme(Zh, "饮", isPrimary: false, LexemeRegister.Literary, article: null, CefrLevel.B2);
        return concept;
    }

    [Fact]
    public void 一个语义对应多个词元_各语言恰一个主词元()
    {
        var concept = CreateDrinkConcept();
        concept.Lexemes.Should().HaveCount(4);
        concept.PrimaryLexeme(De).Lemma.Should().Be("trinken");
        concept.PrimaryLexeme(Zh).Lemma.Should().Be("喝");
    }

    [Fact]
    public void 同语言不允许第二个主词元()
    {
        var concept = CreateDrinkConcept();
        var act = () => concept.AddLexeme(De, "saufen", isPrimary: true, LexemeRegister.Colloquial, article: null, CefrLevel.C1);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.MultiplePrimaryLexemes);
    }

    [Fact]
    public void 同一概念同语言lemma不重复()
    {
        var concept = CreateDrinkConcept();
        var act = () => concept.AddLexeme(De, "trinken", isPrimary: false, LexemeRegister.Neutral, article: null, CefrLevel.A1);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.DuplicateLexeme);
    }

    [Fact]
    public void 还原时缺少主词元的库数据失败()
    {
        var concept = CreateDrinkConcept();
        var lexemes = concept.Lexemes.Where(l => !(l.Language == Zh && l.IsPrimary)).ToArray();
        var act = () => LexicalConcept.Restore(concept.Id, concept.ConceptKey, concept.PartOfSpeech, concept.Kind, lexemes);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.MissingPrimaryLexeme);
    }

    [Fact]
    public void 词元Id由概念语言lemma确定性生成_跨读写稳定()
    {
        var first = CreateDrinkConcept();
        var second = CreateDrinkConcept();
        first.Lexemes.Select(l => l.Id).Should().Equal(second.Lexemes.Select(l => l.Id));
    }

    [Fact]
    public void 冠词仅名词可用()
    {
        var concept = new LexicalConcept(SemanticKey.Of("concept.drink"), PartOfSpeech.Verb, LexemeKind.Word);
        var act = () => concept.AddLexeme(De, "trinken", isPrimary: true, LexemeRegister.Neutral, GermanArticle.Der, CefrLevel.A1);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.ArticleOnlyForNouns);
    }

    [Fact]
    public void 名词展示文本可含冠词_与检索lemma分离()
    {
        var concept = new LexicalConcept(SemanticKey.Of("concept.coffee"), PartOfSpeech.Noun, LexemeKind.Word);
        var lexeme = concept.AddLexeme(De, "Kaffee", isPrimary: true, LexemeRegister.Neutral, GermanArticle.Der, CefrLevel.A1, displayWord: "der Kaffee");
        lexeme.Lemma.Should().Be("Kaffee");
        lexeme.DisplayWord.Should().Be("der Kaffee");
    }

    [Fact]
    public void 单词lemma不允许含连接符_短语lemma用下划线连接词与词()
    {
        var word = new LexicalConcept(SemanticKey.Of("concept.drink"), PartOfSpeech.Verb, LexemeKind.Word);
        var actWord = () => word.AddLexeme(De, "guten_Tag", isPrimary: true, LexemeRegister.Neutral, article: null, CefrLevel.A1);
        actWord.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.WordLemmaMustNotContainJoiner);

        var phrase = new LexicalConcept(SemanticKey.Of("concept.guten.tag"), PartOfSpeech.Other, LexemeKind.Phrase);
        phrase.AddLexeme(De, "guten_Tag", isPrimary: true, LexemeRegister.Neutral, article: null, CefrLevel.A1)
            .Lemma.Should().Be("guten_Tag");
        // 无语间边界的书写系统（如中文）短语 lemma 可不含 '_'
        phrase.AddLexeme(Zh, "你好", isPrimary: true, LexemeRegister.Common, article: null, CefrLevel.A1);
    }

    [Fact]
    public void lemma含空白字符_提示用下划线连接()
    {
        var phrase = new LexicalConcept(SemanticKey.Of("concept.guten.tag"), PartOfSpeech.Other, LexemeKind.Phrase);
        var act = () => phrase.AddLexeme(De, "guten Tag", isPrimary: true, LexemeRegister.Neutral, article: null, CefrLevel.A1);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.LemmaContainsWhitespace);
    }
}

public class ParadigmCellTests
{
    private static readonly LanguageCode De = LanguageCode.Of("de-DE");

    private static Lexeme CreateLexeme()
    {
        var concept = new LexicalConcept(SemanticKey.Of("concept.drink"), PartOfSpeech.Verb, LexemeKind.Word);
        return concept.AddLexeme(De, "trinken", isPrimary: true, LexemeRegister.Neutral, article: null, CefrLevel.A1);
    }

    [Fact]
    public void 槽位Id由概念语言lemma范式槽位确定性生成()
    {
        var a = ParadigmCell.ComputeId("concept.drink", "de-DE", "trinken", "indicative_present", 2);
        var b = ParadigmCell.ComputeId("concept.drink", "de-DE", "trinken", "indicative_present", 2);
        a.Should().Be(b);
        a.Should().NotBe(ParadigmCell.ComputeId("concept.drink", "de-DE", "trinken", "indicative_present", 3));
    }

    [Fact]
    public void 同范式同槽位不允许重复()
    {
        var lexeme = CreateLexeme();
        lexeme.AddParadigmCell(ParadigmCell.ComputeId("concept.drink", "de-DE", "trinken", "indicative_present", 0), "indicative_present", 0, "trinke");
        var act = () => lexeme.AddParadigmCell(ParadigmCell.ComputeId("concept.drink", "de-DE", "trinken", "indicative_present", 0), "indicative_present", 0, "trinke");
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.DuplicateParadigmCell);
    }

    [Fact]
    public void 表层形式不能为空()
    {
        var lexeme = CreateLexeme();
        var act = () => lexeme.AddParadigmCell(ParadigmCell.ComputeId("concept.drink", "de-DE", "trinken", "indicative_present", 0), "indicative_present", 0, " ");
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.EmptySurface);
    }
}

public class InflectionResolverTests
{
    [Fact]
    public void 检索键非唯一_返回全部候选()
    {
        var candidates = new[]
        {
            new InflectionCandidate(ConceptId: Guid.NewGuid(), CellId: Guid.NewGuid(), NormalizedSurface: "sie"),
            new InflectionCandidate(ConceptId: Guid.NewGuid(), CellId: Guid.NewGuid(), NormalizedSurface: "sie"),
            new InflectionCandidate(ConceptId: Guid.NewGuid(), CellId: Guid.NewGuid(), NormalizedSurface: "sie"),
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
