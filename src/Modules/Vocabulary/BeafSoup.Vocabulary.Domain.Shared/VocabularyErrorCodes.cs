namespace BeafSoup.Vocabulary;

public static class VocabularyErrorCodes
{
    public const string EmptyLemma = "Vocabulary:EmptyLemma";
    public const string LemmaContainsWhitespace = "Vocabulary:LemmaContainsWhitespace";
    public const string WordLemmaMustNotContainJoiner = "Vocabulary:WordLemmaMustNotContainJoiner";
    public const string DuplicateLexeme = "Vocabulary:DuplicateLexeme";
    public const string MultiplePrimaryLexemes = "Vocabulary:MultiplePrimaryLexemes";
    public const string MissingPrimaryLexeme = "Vocabulary:MissingPrimaryLexeme";
    public const string ArticleOnlyForNouns = "Vocabulary:ArticleOnlyForNouns";
    public const string NounRequiresArticle = "Vocabulary:NounRequiresArticle";
    public const string EmptyParadigmKey = "Vocabulary:EmptyParadigmKey";
    public const string InvalidSlotOrdinal = "Vocabulary:InvalidSlotOrdinal";
    public const string EmptySurface = "Vocabulary:EmptySurface";
    public const string DuplicateParadigmCell = "Vocabulary:DuplicateParadigmCell";
    public const string InvalidLexemeId = "Vocabulary:InvalidLexemeId";
    public const string DuplicateTag = "Vocabulary:DuplicateTag";
    public const string EmptyTag = "Vocabulary:EmptyTag";
}
