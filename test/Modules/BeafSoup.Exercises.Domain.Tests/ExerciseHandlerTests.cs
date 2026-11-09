using System.Text.Json;
using BeafSoup.SharedKernel;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Exercises;

public class ExerciseHandlerTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static JsonElement AsJson<T>(T value) => JsonSerializer.SerializeToElement(value, Json);

    private static readonly ExerciseRegistry Registry = new(
    [
        new WordDefinitionMcqHandler(), new SentenceClozeMcqHandler(), new SentenceClozeInputHandler(),
        new SentenceTranslationOrderHandler(), new DialogueListeningMcqHandler(), new DialogueClozeHandler(),
    ]);

    // ---------- 选择题按正确答案引用判分，不按显示文本 ----------

    [Fact]
    public void WordDefinitionMcq_CorrectOptionRef_MatchesOnlyDeclaredAnswer()
    {
        var payload = AsJson(new WordDefinitionMcqPayload(1, "der Kaffee", null, "concept.coffee",
        [
            new("opt-trinken", "喝"), new("opt-kaffee", "咖啡"), new("opt-brot", "面包"), new("opt-milch", "牛奶"),
        ], "opt-kaffee"));
        var handler = Registry.Resolve(ExerciseTypeKeys.WordDefinitionMcq, 1);

        handler.Grade(payload, AsJson(new McqAnswer("opt-kaffee"))).Correct.Should().BeTrue();
        handler.Grade(payload, AsJson(new McqAnswer("opt-trinken"))).Correct.Should().BeFalse();
    }

    [Fact]
    public void Mcq_CorrectOptionNotAmongOptions_PayloadInvalid()
    {
        var payload = AsJson(new WordDefinitionMcqPayload(1, "der Kaffee", null, "concept.coffee",
        [new("a", "x"), new("b", "y")], "missing"));
        var act = () => Registry.Resolve(ExerciseTypeKeys.WordDefinitionMcq, 1).ValidatePayload(payload);
        act.Should().Throw<DomainException>().Where(e => e.Code == ExerciseErrorCodes.CorrectOptionNotUnique);
    }

    // ---------- 输入填空：规范化 + 可接受词形集合 ----------

    [Fact]
    public void SentenceClozeInput_DiacriticsCaseAndArticleTolerance_Accepted()
    {
        // 声明容忍前导冠词：der Kaffee / Kaffee / kaffee 均正确；变音符号折叠
        var payload = AsJson(new SentenceClozeInputPayload(
            1, "Ich trinke ____ am Morgen.", 11, 10, null, "de-DE", "TolerateLeadingArticle",
            ["der Kaffee", "Kaffee"]));
        var handler = Registry.Resolve(ExerciseTypeKeys.SentenceClozeInput, 1);

        handler.Grade(payload, AsJson(new InputAnswer("der Kaffee"))).Correct.Should().BeTrue();
        handler.Grade(payload, AsJson(new InputAnswer("Kaffee"))).Correct.Should().BeTrue();
        handler.Grade(payload, AsJson(new InputAnswer("  kaffee "))).Correct.Should().BeTrue();
    }

    [Fact]
    public void SentenceClozeInput_UmlautFolded_ByNormalization()
    {
        var payload = AsJson(new SentenceClozeInputPayload(
            1, "Er fährt nach ____.", 14, 4, null, "de-DE", "Strict", ["Süß"]));
        var handler = Registry.Resolve(ExerciseTypeKeys.SentenceClozeInput, 1);
        handler.Grade(payload, AsJson(new InputAnswer("suss"))).Correct.Should().BeTrue();
        handler.Grade(payload, AsJson(new InputAnswer("Süss"))).Correct.Should().BeTrue();
    }

    [Fact]
    public void SentenceClozeInput_ArticleRequired_WrongWhenMissing()
    {
        var payload = AsJson(new SentenceClozeInputPayload(
            1, "Ich trinke der Kaffee.", 11, 10, null, "de-DE", "Strict", ["der Kaffee"]));
        var handler = Registry.Resolve(ExerciseTypeKeys.SentenceClozeInput, 1);
        handler.Grade(payload, AsJson(new InputAnswer("Kaffee"))).Correct.Should().BeFalse();
    }

    [Fact]
    public void SentenceClozeInput_EmptyAcceptableForms_PayloadInvalid()
    {
        var payload = AsJson(new SentenceClozeInputPayload(
            1, "Ich trinke der Kaffee.", 11, 10, null, "de-DE", "Strict", []));
        var act = () => Registry.Resolve(ExerciseTypeKeys.SentenceClozeInput, 1).ValidatePayload(payload);
        act.Should().Throw<DomainException>().Where(e => e.Code == ExerciseErrorCodes.EmptyAcceptableForms);
    }

    // ---------- 翻译排序：词块各一次 + 实例 ID 顺序完全一致 ----------

    [Fact]
    public void TranslationOrder_ExactInstanceSequenceRequired()
    {
        var payload = AsJson(new SentenceTranslationOrderPayload(1, "你好吗？",
        [
            new("c1", "Wie"), new("c2", "geht"), new("c3", "es"), new("c4", "dir"), new("c5", "?"),
        ], ["c1", "c2", "c3", "c4", "c5"]));
        var handler = Registry.Resolve(ExerciseTypeKeys.SentenceTranslationOrder, 1);

        handler.Grade(payload, AsJson(new OrderAnswer(["c1", "c2", "c3", "c4", "c5"]))).Correct.Should().BeTrue();
        // 顺序错误
        handler.Grade(payload, AsJson(new OrderAnswer(["c2", "c1", "c3", "c4", "c5"]))).Correct.Should().BeFalse();
        // 漏用词块
        handler.Grade(payload, AsJson(new OrderAnswer(["c1", "c2", "c3", "c4"]))).Correct.Should().BeFalse();
        // 重复使用同一块
        handler.Grade(payload, AsJson(new OrderAnswer(["c1", "c1", "c2", "c3", "c4"]))).Correct.Should().BeFalse();
    }

    [Fact]
    public void TranslationOrder_CorrectOrderNotPermutation_PayloadInvalid()
    {
        var payload = AsJson(new SentenceTranslationOrderPayload(1, "x",
        [new("c1", "a"), new("c2", "b")], ["c1", "c1"]));
        var act = () => Registry.Resolve(ExerciseTypeKeys.SentenceTranslationOrder, 1).ValidatePayload(payload);
        act.Should().Throw<DomainException>().Where(e => e.Code == ExerciseErrorCodes.InvalidChunkOrder);
    }

    // ---------- 对话听力 ----------

    [Fact]
    public void DialogueListeningMcq_GradedByOptionRef_ReplayNotAnAnswer()
    {
        var payload = AsJson(new DialogueListeningMcqPayload(1,
        [
            new(0, "Max", "Guten Tag!", "de-DE/s-000001-Max.mp3"),
            new(1, "Kari", "Guten Tag!", "de-DE/s-000001-Kari.mp3"),
        ], "Worum geht es?", [new("a", "Begrüßung"), new("b", "Einkaufen")], "a", 3));
        var handler = Registry.Resolve(ExerciseTypeKeys.DialogueListeningMcq, 1);
        handler.Grade(payload, AsJson(new McqAnswer("a"))).Correct.Should().BeTrue();
        handler.Grade(payload, AsJson(new McqAnswer("b"))).Correct.Should().BeFalse();
    }

    // ---------- 补全对话：responseMode 选择/输入 + 音频规则 ----------

    [Fact]
    public void DialogueCloze_ChoiceMode_GradedByOptionRef()
    {
        var payload = AsJson(new DialogueClozePayload(1, "Choice",
        [
            new(0, "Max", "____!", "de-DE/s-000001-revealed-Max.mp3",
                new(0, 4, [new("a", "Guten Tag"), new("b", "Tschüss")], "a", null, null, "Strict")),
            new(1, "Kari", "Guten Tag!", "de-DE/s-000001-revealed-Kari.mp3", null),
        ]));
        var handler = Registry.Resolve(ExerciseTypeKeys.DialogueCloze, 1);
        handler.ValidatePayload(payload); // 加载/发布时通过
        handler.Grade(payload, AsJson(new DialogueClozeAnswer(0, "a", null))).Correct.Should().BeTrue();
        handler.Grade(payload, AsJson(new DialogueClozeAnswer(0, "b", null))).Correct.Should().BeFalse();
        // 作答指向无空行 → 错误
        handler.Grade(payload, AsJson(new DialogueClozeAnswer(1, "a", null))).Correct.Should().BeFalse();
    }

    [Fact]
    public void DialogueCloze_InputMode_UsesAcceptableForms()
    {
        var payload = AsJson(new DialogueClozePayload(1, "Input",
        [
            new(0, "Max", "____!", "de-DE/s-000001-revealed-Max.mp3",
                new(0, 4, null, null, ["Guten Tag"], "de-DE", "Strict")),
            new(1, "Kari", "Guten Tag!", "de-DE/s-000001-revealed-Kari.mp3", null),
        ]));
        var handler = Registry.Resolve(ExerciseTypeKeys.DialogueCloze, 1);
        handler.Grade(payload, AsJson(new DialogueClozeAnswer(0, null, "guten tag"))).Correct.Should().BeTrue();
        handler.Grade(payload, AsJson(new DialogueClozeAnswer(0, null, "tschüss"))).Correct.Should().BeFalse();
    }

    [Fact]
    public void DialogueCloze_BlankLineMissingRevealedAudio_PayloadInvalid()
    {
        var payload = AsJson(new DialogueClozePayload(1, "Input",
        [
            new(0, "Max", "____!", null, new(0, 4, null, null, ["Guten Tag"], "de-DE", "Strict")),
            new(1, "Kari", "Guten Tag!", "de-DE/s.mp3", null),
        ]));
        var act = () => Registry.Resolve(ExerciseTypeKeys.DialogueCloze, 1).ValidatePayload(payload);
        act.Should().Throw<DomainException>().Where(e => e.Code == ExerciseErrorCodes.InvalidPayload);
    }

    [Fact]
    public void DialogueCloze_ChoiceModeWithoutOptions_PayloadInvalid()
    {
        var payload = AsJson(new DialogueClozePayload(1, "Choice",
        [
            new(0, "Max", "____!", "de-DE/x.mp3", new(0, 4, null, null, ["Guten Tag"], "de-DE", "Strict")),
            new(1, "Kari", "Guten Tag!", "de-DE/y.mp3", null),
        ]));
        var act = () => Registry.Resolve(ExerciseTypeKeys.DialogueCloze, 1).ValidatePayload(payload);
        act.Should().Throw<DomainException>().Where(e => e.Code == ExerciseErrorCodes.IncompatibleResponseMode);
    }

    // ---------- 注册表 ----------

    [Fact]
    public void Resolve_UnknownTypeKey_Throws()
    {
        var act = () => Registry.Resolve("NotARealType", 1);
        act.Should().Throw<DomainException>().Where(e => e.Code == ExerciseErrorCodes.UnknownExerciseType);
    }

    [Fact]
    public void Resolve_WrongPayloadVersion_Throws()
    {
        var act = () => Registry.Resolve(ExerciseTypeKeys.WordDefinitionMcq, 999);
        act.Should().Throw<DomainException>().Where(e => e.Code == ExerciseErrorCodes.UnsupportedPayloadVersion);
    }

    [Fact]
    public void Registry_MissingBuiltInType_Throws()
    {
        var act = () => new ExerciseRegistry([new WordDefinitionMcqHandler()]);
        act.Should().Throw<DomainException>();
    }
}
