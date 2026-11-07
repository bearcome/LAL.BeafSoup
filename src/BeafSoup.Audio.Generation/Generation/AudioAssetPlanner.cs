using BeafSoup.Audio;
using BeafSoup.Audio.Generation.Tts;
using BeafSoup.Localization;
using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary;

namespace BeafSoup.Audio.Generation.Generation;

/// <summary>枚举全部待生成资产：词元 × 已配置音色角色、句子 × 3 角色；对话行校验其 (句子, 角色) 组合已被覆盖（同路径去重）。</summary>
public static class AudioAssetPlanner
{
    public static IReadOnlyList<PlannedAsset> Plan(ValidatedSeed seed, ToolOptions options)
    {
        var normalizer = new GermanTtsTextNormalizer();
        var rolesByLanguage = seed.VoiceRoles
            .GroupBy(v => v.Language.Value)
            .ToDictionary(g => g.Key, g => g.ToDictionary(v => v.Role));

        var plan = new List<PlannedAsset>();

        foreach (var concept in seed.Concepts)
        {
            foreach (var lexeme in concept.Lexemes)
            {
                // 未配置音色角色的语言（如 zh-CN 尚未配角色）跳过单词音频
                if (!rolesByLanguage.TryGetValue(lexeme.Language.Value, out var roles))
                    continue;
                var article = lexeme.Article?.ToString().ToLowerInvariant();
                var lemma = lexeme.Lemma.Replace('_', ' ');
                // 名词单词音频须带冠词朗读（如 "der Kaffee"），无冠词词仅朗读词元；见 audio-asset-spec.md 内容硬规则
                var ttsText = article is null ? lemma : $"{article} {lemma}";
                foreach (var (role, config) in roles)
                {
                    var asset = AudioAsset.Word(lexeme.Language, article, lexeme.Lemma, role);
                    plan.Add(new PlannedAsset(
                        AudioPathBuilder.BuildRelativePath(asset, config.AssetFileToken),
                        normalizer.Normalize(ttsText),
                        config.TtsVoiceName,
                        config.AssetFileToken,
                        SourceKind: "word",
                        SourceKey: lexeme.Id.ToString(),
                        role));
                }
            }
        }

        foreach (var sentence in seed.Sentences)
        {
            var textByLanguage = seed.LocalizedTexts
                .Where(t => t.Key == sentence.Key)
                .ToDictionary(t => t.Language.Value, t => t.Text);
            foreach (var tokenization in sentence.Tokenizations)
            {
                foreach (var (role, config) in rolesByLanguage[tokenization.Language.Value])
                {
                    var asset = AudioAsset.Sentence(tokenization.Language, sentence.Id, role);
                    plan.Add(new PlannedAsset(
                        AudioPathBuilder.BuildRelativePath(asset, config.AssetFileToken),
                        normalizer.Normalize(textByLanguage[tokenization.Language.Value]),
                        config.TtsVoiceName,
                        config.AssetFileToken,
                        SourceKind: "sentence",
                        SourceKey: sentence.Id,
                        role));
                }
            }
        }

        foreach (var dialogue in seed.Dialogues)
        {
            var sentenceById = seed.Sentences.ToDictionary(s => s.Id, s => s);
            var textByKey = seed.LocalizedTexts.ToDictionary(t => (t.Key.Value, t.Language.Value), t => t.Text);
            foreach (var line in dialogue.Lines)
            {
                var sentence = sentenceById[line.SentenceId];
                var language = sentence.Tokenizations.First().Language;
                var config = rolesByLanguage[language.Value][line.VoiceRole];
                var text = textByKey[(sentence.Key.Value, language.Value)];
                var asset = AudioAsset.Sentence(language, sentence.Id, line.VoiceRole);
                plan.Add(new PlannedAsset(
                    AudioPathBuilder.BuildRelativePath(asset, config.AssetFileToken),
                    normalizer.Normalize(text),
                    config.TtsVoiceName,
                    config.AssetFileToken,
                    SourceKind: "dialogue-line",
                    SourceKey: sentence.Id,
                    line.VoiceRole));
            }
        }

        return plan
            .GroupBy(p => p.RelativePath)
            .Select(g => g.FirstOrDefault(p => p.SourceKind == "dialogue-line") ?? g.First())
            .ToArray();
    }
}
