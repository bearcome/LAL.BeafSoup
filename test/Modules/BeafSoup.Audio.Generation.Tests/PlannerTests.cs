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
            Lemmas =
            [
                new()
                {
                    Key = "word.coffee", Language = "de-DE", PartOfSpeech = "Noun", Article = "Der", Cefr = "A1",
                    Translations = new() { ["de-DE"] = "Kaffee" },
                    Forms = [new() { Key = "word.coffee", Surface = "Kaffee", Analyses = [new()] }],
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
    public void 每个单词与句子按三角色生成_路径符合命名规范()
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
        plan.Should().HaveCount(6); // 1 词 × 3 角色 + 1 句 × 3 角色；对话行 Male 组合去重
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
