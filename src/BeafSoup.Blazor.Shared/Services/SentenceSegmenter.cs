namespace BeafSoup.Blazor.Shared.Services;

/// <summary>句子片段：Text 为原文切片；HasLink 表示命中词典、可点击；IsBlank 表示空位占位。</summary>
public sealed record SentenceSegment(string Text, bool HasLink, bool IsBlank);

/// <summary>
/// 句子分词与链接判定：按原文顺序切分单词/空白/标点/空位，并对连续单词做最长短语匹配。
/// 纯函数，词典命中由调用方以谓词提供，便于单元测试。
/// </summary>
public static class SentenceSegmenter
{
    public static IReadOnlyList<SentenceSegment> Build(
        string? text, int blankStart, int blankLength, Func<string, bool> hasEntry)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var hasBlank = blankLength > 0 && blankStart >= 0 && blankStart + blankLength <= text.Length;
        var raw = Tokenize(text, hasBlank ? blankStart : -1, blankLength);
        var result = new List<SentenceSegment>(raw.Count);
        var index = 0;
        while (index < raw.Count)
        {
            var token = raw[index];
            if (token.IsBlank)
            {
                result.Add(new SentenceSegment(new string('_', Math.Max(2, token.Text.Length)), HasLink: false, IsBlank: true));
                index++;
                continue;
            }
            if (!token.IsWord)
            {
                result.Add(new SentenceSegment(token.Text, HasLink: false, IsBlank: false));
                index++;
                continue;
            }

            // 最长匹配：最多 4 个连续单词；命中词典则整体作为一个可点击单元
            var matched = false;
            for (var span = Math.Min(4, raw.Count - index); span >= 1; span--)
            {
                if (!TryPhrase(text, raw, index, span, out var phrase) || !hasEntry(phrase))
                    continue;
                result.Add(new SentenceSegment(phrase, HasLink: true, IsBlank: false));
                // span 个单词之间有 span-1 个空白 token：共消费 2*span-1 个 token
                index += 2 * span - 1;
                matched = true;
                break;
            }
            if (matched)
                continue;

            result.Add(new SentenceSegment(token.Text, HasLink: false, IsBlank: false));
            index++;
        }
        return result;
    }

    /// <summary>从第 start 个 token 起尝试取 span 个单词；单词之间只能是单个空格，不能跨越空位或标点。</summary>
    private static bool TryPhrase(string text, IReadOnlyList<RawToken> tokens, int start, int span, out string phrase)
    {
        phrase = "";
        var end = start;
        for (var offset = 1; offset < span; offset++)
        {
            if (end + 2 >= tokens.Count)
                return false;
            var whitespace = tokens[end + 1];
            var nextWord = tokens[end + 2];
            if (!nextWord.IsWord || whitespace.IsBlank || whitespace.Text != " ")
                return false;
            end += 2;
        }
        var phraseStart = tokens[start];
        var phraseEnd = tokens[end];
        phrase = text[phraseStart.Start..(phraseEnd.Start + phraseEnd.Length)];
        return true;
    }

    /// <summary>把全文切成单词/空白/标点/空位片段；索引保持与原文一致。</summary>
    private static List<RawToken> Tokenize(string text, int blankStart, int blankLength)
    {
        var tokens = new List<RawToken>();
        var position = 0;
        while (position < text.Length)
        {
            if (blankStart >= 0 && position == blankStart)
            {
                tokens.Add(new RawToken(position, blankLength, text[position..(position + blankLength)],
                    IsWord: false, IsBlank: true));
                position += blankLength;
                continue;
            }
            var isLetter = char.IsLetter(text[position]);
            var start = position;
            while (position < text.Length
                   && char.IsLetter(text[position]) == isLetter
                   && !(blankStart >= 0 && position == blankStart))
                position++;
            tokens.Add(new RawToken(start, position - start, text[start..position],
                IsWord: isLetter, IsBlank: false));
        }
        return tokens;
    }

    private sealed record RawToken(int Start, int Length, string Text, bool IsWord, bool IsBlank);
}
