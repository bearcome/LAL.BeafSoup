namespace BeafSoup.Vocabulary;

/// <summary>一条语法分析（格/数/人称/时态/性/级），各维度可空表示不适用。</summary>
public sealed record GrammaticalAnalysis(
    GrammaticalCase? Case = null,
    GrammaticalNumber? Number = null,
    GrammaticalPerson? Person = null,
    GrammaticalTense? Tense = null,
    GrammaticalGender? Gender = null,
    GrammaticalDegree? Degree = null);
