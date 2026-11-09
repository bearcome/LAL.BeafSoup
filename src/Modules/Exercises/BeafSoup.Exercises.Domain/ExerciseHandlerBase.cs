using System.Text.Json;
using BeafSoup.SharedKernel;

namespace BeafSoup.Exercises;

/// <summary>题型处理器基类：负载版本检查 + 反序列化 + 公共校验工具。</summary>
public abstract class ExerciseHandlerBase<TPayload> : IExerciseHandler
    where TPayload : IExercisePayload
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public abstract string TypeKey { get; }
    public abstract int PayloadVersion { get; }

    public void ValidatePayload(JsonElement payload)
    {
        var typed = Deserialize(payload);
        if (typed.Version != PayloadVersion)
            throw new DomainException(ExerciseErrorCodes.UnsupportedPayloadVersion,
                $"题型 {TypeKey} 负载版本 {typed.Version} 不受支持（当前 {PayloadVersion}）。");
        Validate(typed);
    }

    public ExerciseGrade Grade(JsonElement payload, JsonElement answer)
    {
        var typed = Deserialize(payload);
        if (typed.Version != PayloadVersion)
            throw new DomainException(ExerciseErrorCodes.UnsupportedPayloadVersion,
                $"题型 {TypeKey} 负载版本 {typed.Version} 不受支持（当前 {PayloadVersion}）。");
        Validate(typed);
        return Grade(typed, answer);
    }

    protected abstract void Validate(TPayload payload);

    protected abstract ExerciseGrade Grade(TPayload payload, JsonElement answer);

    public ReviewLink? TryGetReviewLink(JsonElement payload) => ExtractReviewLink(Deserialize(payload));

    /// <summary>默认无复习关联；词形/概念关联题型覆写。</summary>
    protected virtual ReviewLink? ExtractReviewLink(TPayload payload) => null;

    protected TPayload Deserialize(JsonElement payload)
    {
        try
        {
            return payload.Deserialize<TPayload>(SerializerOptions)
                ?? throw new DomainException(ExerciseErrorCodes.InvalidPayload, $"题型 {TypeKey} 负载为空。");
        }
        catch (JsonException exception)
        {
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, $"题型 {TypeKey} 负载非法：{exception.Message}");
        }
    }

    protected TAnswer DeserializeAnswer<TAnswer>(JsonElement answer)
    {
        try
        {
            return answer.Deserialize<TAnswer>(SerializerOptions)
                ?? throw new DomainException(ExerciseErrorCodes.InvalidAnswer, "作答内容为空。");
        }
        catch (JsonException exception)
        {
            throw new DomainException(ExerciseErrorCodes.InvalidAnswer, $"作答内容非法：{exception.Message}");
        }
    }

    /// <summary>公共校验：选项 ID 不重复、正确答案引用唯一存在（"只有一个合理答案"的不变量在发布侧保证）。</summary>
    protected static void EnsureUniqueOptionsWithSingleCorrect(IReadOnlyList<ExerciseOption> options, string correctOptionId, string typeKey)
    {
        if (options.Count < 2)
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, $"题型 {typeKey} 至少需要 2 个选项。");
        if (options.Select(o => o.OptionId).Distinct().Count() != options.Count)
            throw new DomainException(ExerciseErrorCodes.DuplicateOptionId, $"题型 {typeKey} 存在重复选项 ID。");
        if (options.Count(o => o.OptionId == correctOptionId) != 1)
            throw new DomainException(ExerciseErrorCodes.CorrectOptionNotUnique,
                $"题型 {typeKey} 的正确答案引用必须唯一存在于选项中。");
        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option.OptionId) || string.IsNullOrWhiteSpace(option.Text))
                throw new DomainException(ExerciseErrorCodes.InvalidPayload, $"题型 {typeKey} 选项 ID/文本不能为空。");
        }
    }

    /// <summary>公共校验：空位跨度必须落在句子文本内。</summary>
    protected static void EnsureBlankWithinSentence(string sentenceText, int blankStart, int blankLength, string typeKey)
    {
        if (blankStart < 0 || blankLength < 1 || blankStart + blankLength > sentenceText.Length)
            throw new DomainException(ExerciseErrorCodes.InvalidBlankSpan,
                $"题型 {typeKey} 空位跨度 [{blankStart}, {blankLength}] 超出句子范围（长度 {sentenceText.Length}）。");
    }

    /// <summary>输入判分：按目标语言规范化后与可接受词形逐一匹配；仅匹配任一项才正确，不做未声明的模糊匹配。</summary>
    protected static bool MatchesAcceptableForms(
        IReadOnlyList<string> acceptableForms, string targetLanguage, ArticleTolerance articleTolerance, string submittedText)
    {
        var normalizer = TextNormalizerRegistry.For(LanguageCode.Of(targetLanguage));
        var submitted = normalizer.Normalize(submittedText, articleTolerance);
        if (submitted.Length == 0)
            return false;
        return acceptableForms.Any(form => normalizer.Normalize(form, articleTolerance) == submitted);
    }
}
