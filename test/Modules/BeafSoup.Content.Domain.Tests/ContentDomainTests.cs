using BeafSoup.Content;
using BeafSoup.SharedKernel;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Content.Domain.Tests;

public class SentenceTokenizationTests
{
    private static readonly LanguageCode De = LanguageCode.Of("de-DE");

    [Fact]
    public void Token按序无重叠且完整覆盖全文()
    {
        // "Guten Tag!"：Guten(0,5) ' '(5,1) Tag(6,3) !(9,1)
        var tokens = new SentenceToken[]
        {
            new(0, 5, SentenceTokenKind.Word),
            new(5, 1, SentenceTokenKind.Whitespace),
            new(6, 3, SentenceTokenKind.Word),
            new(9, 1, SentenceTokenKind.Punctuation),
        };
        var tokenization = new SentenceTokenization(De, 1, tokens, fullTextLength: 10);
        tokenization.Tokens.Should().HaveCount(4);
    }

    [Fact]
    public void 覆盖不完整_抛异常()
    {
        var tokens = new SentenceToken[] { new(0, 5, SentenceTokenKind.Word) };
        var act = () => new SentenceTokenization(De, 1, tokens, fullTextLength: 10);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(ContentErrorCodes.InvalidTokenCoverage);
    }

    [Fact]
    public void Token重叠_抛异常()
    {
        var tokens = new SentenceToken[] { new(0, 5, SentenceTokenKind.Word), new(4, 3, SentenceTokenKind.Word) };
        var act = () => new SentenceTokenization(De, 1, tokens, fullTextLength: 7);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(ContentErrorCodes.InvalidTokenCoverage);
    }

    [Fact]
    public void Token起点越界_抛异常()
    {
        var act = () => new SentenceToken(-1, 2, SentenceTokenKind.Word);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(ContentErrorCodes.InvalidTokenRange);
    }

    [Fact]
    public void 同语言重复标注_抛异常()
    {
        var sentence = new Sentence("s-000001", SemanticKey.Of("sentence.greeting.001"));
        var tokens = new SentenceToken[] { new(0, 1, SentenceTokenKind.Word) };
        sentence.SetTokenization(De, 1, tokens, fullTextLength: 1);
        var act = () => sentence.SetTokenization(De, 2, tokens, fullTextLength: 1);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(ContentErrorCodes.DuplicateTokenization);
    }
}

public class SentenceSpanLinkTests
{
    [Fact]
    public void 修订失配的标注失效()
    {
        var link = new SentenceSpanLink("s-000001", LanguageCode.Of("de-DE"), textRevision: 1, startTokenIndex: 0, tokenCount: 2, SemanticKey.Of("phrase.greeting"));
        link.IsStaleFor(2).Should().BeTrue();
        var act = () => link.EnsureValid(2);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(ContentErrorCodes.StaleAnnotation);
    }

    [Fact]
    public void 越界短语标注_抛异常()
    {
        var act = () => new SentenceSpanLink("s-000001", LanguageCode.Of("de-DE"), 1, startTokenIndex: -1, tokenCount: 2, SemanticKey.Of("phrase.greeting"));
        act.Should().Throw<DomainException>().Which.Code.Should().Be(ContentErrorCodes.InvalidSpanRange);
    }
}

public class DialogueTests
{
    [Fact]
    public void 播放脚本仅为各行台词_绝不包含说话人标签()
    {
        var dialogue = new Dialogue("dlg-0001");
        dialogue.AddLine("Max", "s-000001", VoiceRole.Male);
        dialogue.AddLine("Kari", "s-000002", VoiceRole.Female);

        var texts = new Dictionary<string, string>
        {
            ["s-000001"] = "Guten Tag!",
            ["s-000002"] = "Guten Tag!",
        };
        var script = dialogue.BuildPlaybackScript(id => texts[id]);

        script.Should().Equal("Guten Tag!", "Guten Tag!"); // 补全后整段台词，无 Max:/Kari:
        script.Should().NotContain(s => s.Contains("Max") || s.Contains("Kari"));
    }

    [Fact]
    public void 行引用的句子必须存在()
    {
        var dialogue = new Dialogue("dlg-0001");
        dialogue.AddLine("Max", "s-000001", VoiceRole.Male);
        var act = () => dialogue.BuildPlaybackScript(_ => throw new DomainException(ContentErrorCodes.UnknownSentence, "句子不存在。"));
        act.Should().Throw<DomainException>();
    }
}

public class CourseStructureTests
{
    [Fact]
    public void 课程技能单元课时三级结构()
    {
        var course = new Course("course-a1", SemanticKey.Of("course.german-a1"));
        var unit = new SkillUnit("unit-greetings", SemanticKey.Of("unit.greetings"));
        var lesson = new Lesson("lesson-greetings-01", SemanticKey.Of("lesson.greetings.01"));
        unit.AddLesson(lesson);
        course.AddSkillUnit(unit);

        course.Units.Should().ContainSingle().Which.Lessons.Should().ContainSingle();
    }

    [Fact]
    public void 课时内容有序引用习题实例_题型键占位()
    {
        var lesson = new Lesson("lesson-greetings-01", SemanticKey.Of("lesson.greetings.01"));
        lesson.Content.AddExercise(Guid.NewGuid(), "DialogueCloze");
        lesson.Content.AddExercise(Guid.NewGuid(), "WordDefinitionMcq");
        lesson.Content.Refs.Should().HaveCount(2);
    }

    [Fact]
    public void 空题型键_抛异常()
    {
        var lesson = new Lesson("lesson-greetings-01", SemanticKey.Of("lesson.greetings.01"));
        var act = () => lesson.Content.AddExercise(Guid.NewGuid(), "");
        act.Should().Throw<DomainException>().Which.Code.Should().Be(ContentErrorCodes.EmptyExerciseTypeKey);
    }
}

public class EssayTests
{
    [Fact]
    public void 短文由句子拼接_不允许重复句子()
    {
        var essay = new Essay("essay-0001");
        essay.AppendSentence("s-000001");
        var act = () => essay.AppendSentence("s-000001");
        act.Should().Throw<DomainException>().Which.Code.Should().Be(ContentErrorCodes.DuplicateSentence);
    }
}
