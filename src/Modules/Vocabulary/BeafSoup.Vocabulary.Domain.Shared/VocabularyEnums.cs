namespace BeafSoup.Vocabulary;

public enum PartOfSpeech { Noun, Verb, Adjective, Adverb, Pronoun, Preposition, Conjunction, Determiner, Other }

/// <summary>德语名词冠词（仅名词使用；其他词性为 null）。</summary>
public enum GermanArticle { Der, Die, Das }

public enum CefrLevel { A1, A2, B1, B2, C1, C2 }

public enum GrammaticalCase { Nominative, Accusative, Dative, Genitive }
public enum GrammaticalNumber { Singular, Plural }
public enum GrammaticalPerson { First, Second, Third }
public enum GrammaticalTense { Present, Past, Perfect, Future }
public enum GrammaticalGender { Masculine, Feminine, Neuter }
public enum GrammaticalDegree { Positive, Comparative, Superlative }
