using System.Net.Http.Json;
using BeafSoup.Content;
using BeafSoup.Identity;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Api.Tests;

/// <summary>内容库验收：对话/短文/例句经只读 API 下发，句子带词元概念标注与音频路径。</summary>
[Collection("api")]
public sealed class ContentLibraryApiTests(ApiTestFixture fixture)
{
    private static string LocateSeedDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "data", "seed")))
            directory = directory.Parent;
        return directory is null
            ? throw new DirectoryNotFoundException("找不到仓库 data/seed 目录。")
            : Path.Combine(directory.FullName, "data", "seed");
    }

    private async Task<HttpClient> NewLearnerClientAsync(string email)
    {
        await DbMigrator.DatabaseSeeder.SeedAsync(
            fixture.ConnectionString, LocateSeedDirectory(), replaceExisting: true);
        var client = fixture.CreateClient();
        var tokens = await fixture.RegisterAsync(client, email);
        ApiTestFixture.Authenticate(client, tokens);
        return client;
    }

    [Fact]
    public async Task 对话库按行下发文本与各自音色音频()
    {
        var client = await NewLearnerClientAsync("library-dialogue@example.com");

        var dialogues = await client.GetFromJsonAsync<List<DialogueDto>>("/api/content/dialogues?language=de-DE");

        dialogues.Should().HaveCount(30); // 每个课时一个对话
        var greeting = dialogues!.Single(d => d.DialogueId == "dlg-0001");
        greeting.Lines.Should().HaveCount(3);
        greeting.Lines.Select(l => l.VoiceRole).Should().Equal("Male", "Female", "Child");
        greeting.Lines.Select(l => l.SpeakerLabel).Should().Equal("Max", "Mia", "Lars");
        greeting.Lines.Should().OnlyContain(l => !string.IsNullOrWhiteSpace(l.Text) && !string.IsNullOrWhiteSpace(l.AudioUrl));
        // 音频路径按行自身音色解析；说话人标签绝不进入文件名
        var tokensByRole = new Dictionary<string, string> { ["Male"] = "Max", ["Female"] = "Mia", ["Child"] = "Lars" };
        greeting.Lines.Should().OnlyContain(l => l.AudioUrl == $"de-DE/{l.SentenceId}-{tokensByRole[l.VoiceRole]}.mp3");
    }

    [Fact]
    public async Task 短文库按句下发并保留顺序()
    {
        var client = await NewLearnerClientAsync("library-essay@example.com");

        var essays = await client.GetFromJsonAsync<List<EssayDto>>("/api/content/essays?language=de-DE");

        essays.Should().HaveCount(15); // 每两课一篇短文
        var first = essays!.Single(e => e.EssayId == "essay-0001");
        first.Sentences.Should().HaveCount(4);
        first.Sentences.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s.Text) && !string.IsNullOrWhiteSpace(s.AudioUrl));
        first.Sentences.Select(s => s.SentenceId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task 例句按概念标注精确过滤()
    {
        var client = await NewLearnerClientAsync("library-examples@example.com");

        var sentences = await client.GetFromJsonAsync<List<LibrarySentenceDto>>(
            "/api/content/sentences?language=de-DE&conceptKey=concept.coffee");

        sentences.Should().NotBeEmpty();
        sentences!.Should().OnlyContain(s => s.Text.Contains("Kaffee"));
        sentences!.SelectMany(s => s.Tokens).Should().Contain(t => t.ConceptKey == "concept.coffee");    }
}
