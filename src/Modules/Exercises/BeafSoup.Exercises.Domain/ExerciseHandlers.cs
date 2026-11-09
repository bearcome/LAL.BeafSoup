using System.Text.Json;
using BeafSoup.SharedKernel;

namespace BeafSoup.Exercises;

// ---------- 负载与作答 DTO（JSON 序列化持久化；负载版本显式携带） ----------

public sealed record WordDefinitionMcqPayload(
    int Version, string WordText, string? AudioUrl, string ConceptKey,
    IReadOnlyList<ExerciseOption> Options, string CorrectOptionId) : IExercisePayload;
public sealed record McqAnswer(string OptionId);

public sealed record SentenceClozeMcqPayload(
    int Version, string SentenceText, int BlankStart, int BlankLength, string? AudioUrl,
    IReadOnlyList<ExerciseOption> Options, string CorrectOptionId,
    Guid? CorrectCellId = null, string? ConceptKey = null) : IExercisePayload;
// 作答复用 McqAnswer

public sealed record SentenceClozeInputPayload(
    int Version, string SentenceText, int BlankStart, int BlankLength, string? AudioUrl,
    string TargetLanguage, string ArticleTolerance, IReadOnlyList<string> AcceptableForms,
    Guid? CorrectCellId = null, string? ConceptKey = null) : IExercisePayload;
public sealed record InputAnswer(string Text);

public sealed record TranslationChunk(string ChunkId, string Text);
public sealed record SentenceTranslationOrderPayload(
    int Version, string PromptText, IReadOnlyList<TranslationChunk> Chunks, IReadOnlyList<string> CorrectOrder) : IExercisePayload;
public sealed record OrderAnswer(IReadOnlyList<string> ChunkIds);

public sealed record DialogueLineRef(int LineIndex, string SpeakerLabel, string Text, string? RevealedAudioUrl);
public sealed record DialogueListeningMcqPayload(
    int Version, IReadOnlyList<DialogueLineRef> Lines, string QuestionText,
    IReadOnlyList<ExerciseOption> Options, string CorrectOptionId, int MaxReplayCount) : IExercisePayload;

public sealed record DialogueClozeBlank(
    int Start, int Length, IReadOnlyList<ExerciseOption>? Options, string? CorrectOptionId,
    IReadOnlyList<string>? AcceptableForms, string? TargetLanguage, string ArticleTolerance,
    Guid? CorrectCellId = null, string? ConceptKey = null);
public sealed record DialogueClozeLine(int LineIndex, string SpeakerLabel, string Text, string? RevealedAudioUrl, DialogueClozeBlank? Blank);
public sealed record DialogueClozePayload(
    int Version, string ResponseMode, IReadOnlyList<DialogueClozeLine> Lines) : IExercisePayload;
public sealed record DialogueClozeAnswer(int LineIndex, string? OptionId, string? Text);

/// <summary>六种题型的处理器：判分依据 = 负载声明的正确答案引用/可接受词形/正确顺序，绝不按显示文本猜测。</summary>
public sealed class WordDefinitionMcqHandler : ExerciseHandlerBase<WordDefinitionMcqPayload>
{
    public override string TypeKey => ExerciseTypeKeys.WordDefinitionMcq;
    public override int PayloadVersion => 1;

    protected override void Validate(WordDefinitionMcqPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.WordText) || string.IsNullOrWhiteSpace(payload.ConceptKey))
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "单词释义题必须携带单词文本与概念键。");
        EnsureUniqueOptionsWithSingleCorrect(payload.Options, payload.CorrectOptionId, TypeKey);
    }

    protected override ExerciseGrade Grade(WordDefinitionMcqPayload payload, JsonElement answer)
        => new(DeserializeAnswer<McqAnswer>(answer).OptionId == payload.CorrectOptionId, null);
}

public sealed class SentenceClozeMcqHandler : ExerciseHandlerBase<SentenceClozeMcqPayload>
{
    protected override ReviewLink? ExtractReviewLink(SentenceClozeMcqPayload payload)
        => payload.CorrectCellId is null && payload.ConceptKey is null ? null : new(payload.CorrectCellId, payload.ConceptKey);

    public override string TypeKey => ExerciseTypeKeys.SentenceClozeMcq;
    public override int PayloadVersion => 1;

    protected override void Validate(SentenceClozeMcqPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.SentenceText))
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "句子填空题必须携带句子文本。");
        EnsureBlankWithinSentence(payload.SentenceText, payload.BlankStart, payload.BlankLength, TypeKey);
        EnsureUniqueOptionsWithSingleCorrect(payload.Options, payload.CorrectOptionId, TypeKey);
    }

    protected override ExerciseGrade Grade(SentenceClozeMcqPayload payload, JsonElement answer)
        => new(DeserializeAnswer<McqAnswer>(answer).OptionId == payload.CorrectOptionId, null);
}

public sealed class SentenceClozeInputHandler : ExerciseHandlerBase<SentenceClozeInputPayload>
{
    protected override ReviewLink? ExtractReviewLink(SentenceClozeInputPayload payload)
        => payload.CorrectCellId is null && payload.ConceptKey is null ? null : new(payload.CorrectCellId, payload.ConceptKey);

    public override string TypeKey => ExerciseTypeKeys.SentenceClozeInput;
    public override int PayloadVersion => 1;

    protected override void Validate(SentenceClozeInputPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.SentenceText))
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "输入填空题必须携带句子文本。");
        EnsureBlankWithinSentence(payload.SentenceText, payload.BlankStart, payload.BlankLength, TypeKey);
        if (payload.AcceptableForms.Count == 0)
            throw new DomainException(ExerciseErrorCodes.EmptyAcceptableForms, "输入填空题必须声明可接受词形集合。");
        if (!Enum.TryParse<ArticleTolerance>(payload.ArticleTolerance, out _))
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, $"未知冠词容忍策略 '{payload.ArticleTolerance}'。");
        // 目标语言必须有已验证的文本规范化处理器（未覆盖语言明确失败，不静默套用通用规则）
        TextNormalizerRegistry.For(LanguageCode.Of(payload.TargetLanguage));
    }

    protected override ExerciseGrade Grade(SentenceClozeInputPayload payload, JsonElement answer)
    {
        var submitted = DeserializeAnswer<InputAnswer>(answer);
        var tolerance = Enum.Parse<ArticleTolerance>(payload.ArticleTolerance);
        return new(MatchesAcceptableForms(payload.AcceptableForms, payload.TargetLanguage, tolerance, submitted.Text), null);
    }
}

public sealed class SentenceTranslationOrderHandler : ExerciseHandlerBase<SentenceTranslationOrderPayload>
{
    public override string TypeKey => ExerciseTypeKeys.SentenceTranslationOrder;
    public override int PayloadVersion => 1;

    protected override void Validate(SentenceTranslationOrderPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.PromptText))
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "翻译排序题必须携带题干句子。");
        if (payload.Chunks.Select(c => c.ChunkId).Distinct().Count() != payload.Chunks.Count)
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "翻译排序题词块 ID 必须唯一（重复文本也是不同实例）。");
        // 正确顺序必须是词块 ID 的排列：全部各使用一次
        if (payload.CorrectOrder.Count != payload.Chunks.Count
            || payload.CorrectOrder.OrderBy(id => id).SequenceEqual(payload.Chunks.Select(c => c.ChunkId).OrderBy(id => id)) is false)
            throw new DomainException(ExerciseErrorCodes.InvalidChunkOrder, "翻译排序题正确顺序必须是全部词块各一次的重排。");
    }

    protected override ExerciseGrade Grade(SentenceTranslationOrderPayload payload, JsonElement answer)
    {
        var submitted = DeserializeAnswer<OrderAnswer>(answer);
        // 判定：词块各使用一次且实例 ID 顺序与负载正确顺序完全一致
        var correct = submitted.ChunkIds.Count == payload.CorrectOrder.Count
            && submitted.ChunkIds.SequenceEqual(payload.CorrectOrder);
        return new(correct, null);
    }
}

public sealed class DialogueListeningMcqHandler : ExerciseHandlerBase<DialogueListeningMcqPayload>
{
    public override string TypeKey => ExerciseTypeKeys.DialogueListeningMcq;
    public override int PayloadVersion => 1;

    protected override void Validate(DialogueListeningMcqPayload payload)
    {
        if (payload.Lines.Count < 2)
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "对话听力题至少需要两行对话。");
        if (payload.Lines.Select(l => l.LineIndex).Distinct().Count() != payload.Lines.Count)
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "对话行序号必须唯一。");
        if (string.IsNullOrWhiteSpace(payload.QuestionText))
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "对话听力题必须携带理解题题干。");
        if (payload.MaxReplayCount < 1)
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "最大重播次数必须不小于 1。");
        EnsureUniqueOptionsWithSingleCorrect(payload.Options, payload.CorrectOptionId, TypeKey);
    }

    protected override ExerciseGrade Grade(DialogueListeningMcqPayload payload, JsonElement answer)
        => new(DeserializeAnswer<McqAnswer>(answer).OptionId == payload.CorrectOptionId, null);
}

public sealed class DialogueClozeHandler : ExerciseHandlerBase<DialogueClozePayload>
{
    protected override ReviewLink? ExtractReviewLink(DialogueClozePayload payload)
    {
        var blank = payload.Lines.Select(l => l.Blank).FirstOrDefault(b => b is not null);
        return blank is null || (blank.CorrectCellId is null && blank.ConceptKey is null)
            ? null
            : new(blank.CorrectCellId, blank.ConceptKey);
    }

    public override string TypeKey => ExerciseTypeKeys.DialogueCloze;
    public override int PayloadVersion => 1;

    protected override void Validate(DialogueClozePayload payload)
    {
        if (!Enum.TryParse<DialogueClozeResponseMode>(payload.ResponseMode, out var mode))
            throw new DomainException(ExerciseErrorCodes.IncompatibleResponseMode, $"未知作答模式 '{payload.ResponseMode}'。");
        if (payload.Lines.Count < 2)
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "补全对话至少需要两行对话。");
        var blanks = payload.Lines.Where(l => l.Blank is not null).ToArray();
        if (blanks.Length != 1)
            throw new DomainException(ExerciseErrorCodes.InvalidPayload, "补全对话负载必须恰好包含一个空位。");
        foreach (var line in payload.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.SpeakerLabel))
                throw new DomainException(ExerciseErrorCodes.InvalidPayload, "说话人标签不能为空（仅展示，绝不进入 TTS）。");
            // 音频规则：每行必须是补全后台词（revealed）资产；播放补全后的完整对话，结构上不朗读说话人标签
            if (string.IsNullOrWhiteSpace(line.RevealedAudioUrl))
                throw new DomainException(ExerciseErrorCodes.InvalidPayload, "补全对话每行都必须提供补全版音频（revealed 资产）。");
            if (line.Blank is not null)
                EnsureBlankWithinSentence(line.Text, line.Blank.Start, line.Blank.Length, TypeKey);
        }
        var blank = blanks[0].Blank!;
        switch (mode)
        {
            case DialogueClozeResponseMode.Choice:
                if (blank.Options is null || blank.AcceptableForms is not null)
                    throw new DomainException(ExerciseErrorCodes.IncompatibleResponseMode,
                        "Choice 模式的空位必须提供选项且不得提供可接受词形。");
                EnsureUniqueOptionsWithSingleCorrect(blank.Options, blank.CorrectOptionId ?? "", TypeKey);
                break;
            case DialogueClozeResponseMode.Input:
                if (blank.AcceptableForms is null || blank.AcceptableForms.Count == 0 || blank.Options is not null)
                    throw new DomainException(ExerciseErrorCodes.IncompatibleResponseMode,
                        "Input 模式的空位必须提供可接受词形且不得提供选项。");
                TextNormalizerRegistry.For(LanguageCode.Of(blank.TargetLanguage ?? throw new DomainException(
                    ExerciseErrorCodes.InvalidPayload, "Input 模式必须声明目标语言。")));
                if (!Enum.TryParse<ArticleTolerance>(blank.ArticleTolerance, out _))
                    throw new DomainException(ExerciseErrorCodes.InvalidPayload, $"未知冠词容忍策略 '{blank.ArticleTolerance}'。");
                break;
        }
    }

    protected override ExerciseGrade Grade(DialogueClozePayload payload, JsonElement answer)
    {
        var submitted = DeserializeAnswer<DialogueClozeAnswer>(answer);
        var blankLine = payload.Lines.FirstOrDefault(l => l.Blank is not null);
        if (blankLine is null || blankLine.Blank is null || submitted.LineIndex != blankLine.LineIndex)
            return new(false, null);
        var mode = Enum.Parse<DialogueClozeResponseMode>(payload.ResponseMode);
        return mode switch
        {
            DialogueClozeResponseMode.Choice => new(submitted.OptionId == blankLine.Blank.CorrectOptionId, null),
            DialogueClozeResponseMode.Input => new(
                MatchesAcceptableForms(
                    blankLine.Blank.AcceptableForms!, blankLine.Blank.TargetLanguage!,
                    Enum.Parse<ArticleTolerance>(blankLine.Blank.ArticleTolerance), submitted.Text ?? ""), null),
            _ => throw new DomainException(ExerciseErrorCodes.IncompatibleResponseMode, $"未知作答模式 '{payload.ResponseMode}'。"),
        };
    }
}
