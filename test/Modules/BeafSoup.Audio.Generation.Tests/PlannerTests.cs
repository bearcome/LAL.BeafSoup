using BeafSoup.Audio.Generation.Generation;
using BeafSoup.Audio.Generation.Seed;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Audio.Generation.Tests;

public class AudioAssetPlannerTests
{
    private static ValidatedSeed BuildSeed()
    {
        var seed = new SeedData
        {
            VoiceRoles =
            [
                new() { Language = "de-DE", Role = "Male", AssetFileToken = "Max", TtsVoiceName = "de-DE-ConradNeural" },
                new() { Language = "de-DE", Role = "Female", AssetFileToken = "Mia", TtsVoiceName = "de-DE-KatjaNeural" },
                new() { Language = "de-DE", Role = "Child", AssetFileToken = "Lars", TtsVoiceName = "de-DE-KasperNeural" },
            ],
            Concepts =
            [
                new()
                {
                    Key = "concept.coffee", PartOfSpeech = "Noun", Kind = "Word",
                    Lexemes = new()
                    {
                        ["de-DE"] =
                        [
                            new() { Lemma = "Kaffee", Display = "der Kaffee", IsPrimary = true, Register = "Neutral", Article = "Der", Cefr = "A1" },
                        ],
                    },
                },
            ],
            Sentences =
            [
                new()
                {
                    Id = "s-000001", Key = "sentence.greeting.001",
                    Localizations =
                    [
                        new() { Language = "de-DE", Text = "Guten Tag!", Revision = 1, Tokens = [new() { Start = 0, Length = 5, Kind = "Word" }, new() { Start = 5, Length = 5, Kind = "Punctuation" }] },
                    ],
                },
            ],
            Dialogues =
            [
                new()
                {
                    Id = "dlg-0001",
                    Lines =
                    [
                        new() { SpeakerLabel = "Max", SentenceId = "s-000001", VoiceRole = "Male" },
                    ],
                },
            ],
        };
        var result = SeedValidator.Validate(seed);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors));
        return result.Value!;
    }

    [Fact]
    public void 每个词元与句子按三角色生成_路径符合命名规范()
    {
        var options = new ToolOptions
        {
            SeedDirectory = "",
            OutputDirectory = "",
            CefrRates = new() { ["A1"] = "-10%" },
            RoleProsody = new(),
        };
        var plan = AudioAssetPlanner.Plan(BuildSeed(), options);

        plan.Should().Contain(p => p.RelativePath == "de-DE/der-Kaffee-Max.mp3");
        plan.Should().Contain(p => p.RelativePath == "de-DE/der-Kaffee-Mia.mp3");
        plan.Should().Contain(p => p.RelativePath == "de-DE/s-000001-Lars.mp3");
        plan.Should().HaveCount(6); // 1 词元 × 3 角色 + 1 句 × 3 角色；对话行 Male 组合去重
    }

    [Fact]
    public void 名词单词音频TTS文本带冠词朗读_其他词性仅词元()
    {
        var seed = new SeedData
        {
            VoiceRoles =
            [
                new() { Language = "de-DE", Role = "Male", AssetFileToken = "Max", TtsVoiceName = "de-DE-ConradNeural" },
                new() { Language = "de-DE", Role = "Female", AssetFileToken = "Mia", TtsVoiceName = "de-DE-KatjaNeural" },
                new() { Language = "de-DE", Role = "Child", AssetFileToken = "Lars", TtsVoiceName = "de-DE-KasperNeural" },
            ],
            Concepts =
            [
                new()
                {
                    Key = "concept.coffee", PartOfSpeech = "Noun", Kind = "Word",
                    Lexemes = new()
                    {
                        ["de-DE"] = [new() { Lemma = "Kaffee", IsPrimary = true, Register = "Neutral", Article = "Der", Cefr = "A1" }],
                    },
                },
                new()
                {
                    Key = "concept.drink", PartOfSpeech = "Verb", Kind = "Word",
                    Lexemes = new()
                    {
                        ["de-DE"] = [new() { Lemma = "trinken", IsPrimary = true, Register = "Neutral", Cefr = "A1" }],
                    },
                },
            ],
        };
        var result = SeedValidator.Validate(seed);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors));
        var options = new ToolOptions { SeedDirectory = "", OutputDirectory = "", CefrRates = [], RoleProsody = [] };

        var plan = AudioAssetPlanner.Plan(result.Value!, options);

        var coffeeKey = result.Value!.Concepts.Single(c => c.ConceptKey.Value == "concept.coffee").Lexemes.Single().Id.ToString();
        var drinkKey = result.Value!.Concepts.Single(c => c.ConceptKey.Value == "concept.drink").Lexemes.Single().Id.ToString();
        plan.Where(p => p.SourceKind == "word" && p.SourceKey == coffeeKey)
            .Should().OnlyContain(p => p.TtsText == "der Kaffee");
        plan.Where(p => p.SourceKind == "word" && p.SourceKey == drinkKey)
            .Should().OnlyContain(p => p.TtsText == "trinken");
    }

    [Fact]
    public void 短语音频_TTS还原空格_文件名转连字符()
    {
        var seed = new SeedData
        {
            VoiceRoles =
            [
                new() { Language = "de-DE", Role = "Male", AssetFileToken = "Max", TtsVoiceName = "de-DE-ConradNeural" },
                new() { Language = "de-DE", Role = "Female", AssetFileToken = "Mia", TtsVoiceName = "de-DE-KatjaNeural" },
                new() { Language = "de-DE", Role = "Child", AssetFileToken = "Lars", TtsVoiceName = "de-DE-KasperNeural" },
            ],
            Concepts =
            [
                new()
                {
                    Key = "concept.guten.tag", PartOfSpeech = "Other", Kind = "Phrase",
                    Lexemes = new()
                    {
                        ["de-DE"] = [new() { Lemma = "guten_Tag", IsPrimary = true, Register = "Neutral", Cefr = "A1" }],
                    },
                },
            ],
        };
        var result = SeedValidator.Validate(seed);
        result.IsValid.Should().BeTrue(string.Join(";", result.Errors));
        var options = new ToolOptions { SeedDirectory = "", OutputDirectory = "", CefrRates = [], RoleProsody = [] };

        var plan = AudioAssetPlanner.Plan(result.Value!, options);

        plan.Should().OnlyContain(p => p.RelativePath == "de-DE/guten-Tag-Max.mp3"
            || p.RelativePath == "de-DE/guten-Tag-Mia.mp3" || p.RelativePath == "de-DE/guten-Tag-Lars.mp3");
        plan.Should().OnlyContain(p => p.TtsText == "guten Tag");
    }

    [Fact]
    public void 对话行角色组合全部被计划覆盖_且说话人标签不进入TTS文本()
    {
        var options = new ToolOptions { SeedDirectory = "", OutputDirectory = "", CefrRates = [], RoleProsody = [] };
        var plan = AudioAssetPlanner.Plan(BuildSeed(), options);
        plan.Where(p => p.SourceKind == "dialogue-line").Should().ContainSingle()
            .Which.TtsText.Should().Be("Guten Tag!"); // 仅台词，无 "Max:"
    }
}
