using BeafSoup.Audio;
using BeafSoup.Audio.Generation.Tts;
using BeafSoup.Localization;
using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary;

namespace BeafSoup.Audio.Generation.Generation;

/// <summary>枚举全部待生成资产：词 × 3 角色、句子 × 3 角色；对话行校验其 (句子, 角色) 组合已被覆盖（同路径去重）。</summary>
public static class AudioAssetPlanner
{
    public static IReadOnlyList<PlannedAsset> Plan(ValidatedSeed seed, ToolOptions options)
    {
        var normalizer = new GermanTtsTextNormalizer();
        var rolesByLanguage = seed.VoiceRoles
            .GroupBy(v => v.Language.Value)
            .ToDictionary(g => g.Key, g => g.ToDictionary(v => v.Role));

        var plan = new List<PlannedAsset>();

        foreach (var lemma in seed.Lemmas)
        {
            var lemmaText = seed.LocalizedTexts.First(t => t.Key == lemma.LemmaKey && t.Language == lemma.Language).Text;
            var article = lemma.Article?.ToString().ToLowerInvariant();
            foreach (var (role, config) in rolesByLanguage[lemma.Language.Value])
            {
                var asset = AudioAsset.Word(lemma.Language, article, lemmaText, role);
                plan.Add(new PlannedAsset(
                    AudioPathBuilder.BuildRelativePath(asset, config.AssetFileToken),
                    normalizer.Normalize(lemmaText),
                    config.TtsVoiceName,
                    config.AssetFileToken,
                    SourceKind: "word",
                    SourceKey: lemma.LemmaKey.Value,
                    role));
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
