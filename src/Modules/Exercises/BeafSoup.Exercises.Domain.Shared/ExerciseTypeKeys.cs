namespace BeafSoup.Exercises;

/// <summary>题型注册表键：新题型只加常量 + 注册 handler，课程引擎零改动。MockExam 是整卷编排，不是题型键。</summary>
public static class ExerciseTypeKeys
{
    public const string WordDefinitionMcq = "WordDefinitionMcq";
    public const string SentenceClozeMcq = "SentenceClozeMcq";
    public const string SentenceClozeInput = "SentenceClozeInput";
    public const string SentenceTranslationOrder = "SentenceTranslationOrder";
    public const string DialogueListeningMcq = "DialogueListeningMcq";
    public const string DialogueCloze = "DialogueCloze";

    public static readonly IReadOnlyCollection<string> All =
    [
        WordDefinitionMcq, SentenceClozeMcq, SentenceClozeInput,
        SentenceTranslationOrder, DialogueListeningMcq, DialogueCloze,
    ];
}
