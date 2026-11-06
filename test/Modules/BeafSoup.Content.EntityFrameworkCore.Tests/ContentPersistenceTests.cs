using BeafSoup.Content.EntityFrameworkCore;
using BeafSoup.SharedKernel;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeafSoup.Content.EntityFrameworkCore.Tests;

/// <summary>临时 SQLite 文件库：每个测试实例独立文件，避免用例间相互污染。</summary>
public sealed class TempContentDatabase : IAsyncDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"content-{Guid.NewGuid():N}.db");

    public async Task<ContentDbContext> CreateAndMigrateAsync()
    {
        var context = CreateContext();
        await context.Database.MigrateAsync();
        return context;
    }

    public ContentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseSqlite($"Data Source={_path}", sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Content"))
            .Options;
        return new ContentDbContext(options);
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
        return ValueTask.CompletedTask;
    }
}

public class ContentPersistenceTests
{
    private static Sentence BuildGreeting()
    {
        const string text = "Guten Tag!";
        var sentence = new Sentence("s-000001", SemanticKey.Of("sentence.greeting.001"));
        sentence.SetTokenization(LanguageCode.Of("de-DE"), 1, new SentenceToken[]
        {
            new(0, 5, SentenceTokenKind.Word),
            new(5, 1, SentenceTokenKind.Whitespace),
            new(6, 3, SentenceTokenKind.Word),
            new(9, 1, SentenceTokenKind.Punctuation),
        }, text.Length);
        return sentence;
    }

    [Fact]
    public async Task 句子分词往返一致_词元顺序与修订号保留()
    {
        var sentence = BuildGreeting();
        await using var database = new TempContentDatabase();

        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.Sentences.Add(ContentPersistenceMapper.ToRow(sentence));
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var restored = ContentPersistenceMapper.ToDomain(await read.Sentences.SingleAsync());

        restored.Id.Should().Be(sentence.Id);
        var tokenization = restored.Tokenizations.Single();
        tokenization.TextRevision.Should().Be(1);
        tokenization.Tokens.Should().BeEquivalentTo(sentence.Tokenizations[0].Tokens, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task 对话行顺序保留_说话人与角色逐行还原()
    {
        var dialogue = new Dialogue("dlg-0001");
        dialogue.AddLine("Max", "s-000001", VoiceRole.Male);
        dialogue.AddLine("Kari", "s-000002", VoiceRole.Female);
        dialogue.AddLine("Max", "s-000003", VoiceRole.Male);
        await using var database = new TempContentDatabase();

        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.Dialogues.Add(ContentPersistenceMapper.ToRow(dialogue));
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var restored = ContentPersistenceMapper.ToDomain(await read.Dialogues.SingleAsync());

        restored.Lines.Select(l => (l.SpeakerLabel, l.SentenceId, l.VoiceRole))
            .Should().Equal(("Max", "s-000001", VoiceRole.Male), ("Kari", "s-000002", VoiceRole.Female), ("Max", "s-000003", VoiceRole.Male));
    }

    [Fact]
    public async Task 课程树顺序保留_课时习题引用往返一致()
    {
        var course = new Course("course-de-a1", SemanticKey.Of("course.de.a1.title"));
        var unit = new SkillUnit("unit-001", SemanticKey.Of("unit.001.title"));
        var lesson = new Lesson("lesson-0001", SemanticKey.Of("lesson.0001.title"));
        var firstExerciseId = Guid.NewGuid();
        var secondExerciseId = Guid.NewGuid();
        lesson.Content.AddExercise(firstExerciseId, "WordDefinitionMcq");
        lesson.Content.AddExercise(secondExerciseId, "SentenceClozeMcq");
        unit.AddLesson(lesson);
        course.AddSkillUnit(unit);
        await using var database = new TempContentDatabase();

        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.Courses.Add(ContentPersistenceMapper.ToRow(course));
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var restored = ContentPersistenceMapper.ToDomain(await read.Courses.SingleAsync());

        var restoredLesson = restored.Units.Single().Lessons.Single();
        restoredLesson.Content.Refs.Should().Equal(
            new ExerciseRef(firstExerciseId, "WordDefinitionMcq"),
            new ExerciseRef(secondExerciseId, "SentenceClozeMcq"));
    }
}
