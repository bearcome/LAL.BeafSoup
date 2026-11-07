namespace BeafSoup.Vocabulary;

public enum PartOfSpeech { Noun, Verb, Adjective, Adverb, Pronoun, Preposition, Conjunction, Determiner, Other }

/// <summary>词条种类：单词或短语；短语的 lemma 用 `_` 连接词与词（如 guten_Tag），与单词区分。</summary>
public enum LexemeKind { Word, Phrase }

/// <summary>词元语体（同义/语体差异的词元归属同一概念）。</summary>
public enum LexemeRegister { Neutral, Elevated, Colloquial, Common, Literary }

/// <summary>德语名词冠词（仅名词使用；其他词性为 null）。</summary>
public enum GermanArticle { Der, Die, Das }

public enum CefrLevel { A1, A2, B1, B2, C1, C2 }
