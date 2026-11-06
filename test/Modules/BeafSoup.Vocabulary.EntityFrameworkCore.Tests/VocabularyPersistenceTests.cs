using BeafSoup.SharedKernel;
using BeafSoup.Vocabulary.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BeafSoup.Vocabulary.EntityFrameworkCore.Tests;

/// <summary>临时 SQLite 文件库：每个测试实例独立文件，避免用例间相互污染。</summary>
public sealed class TempVocabularyDatabase : IAsyncDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"voc-{Guid.NewGuid():N}.db");

    public async Task<VocabularyDbContext> CreateAndMigrateAsync()
    {
        var context = CreateContext();
        await context.Database.MigrateAsync();
        return context;
    }

    public VocabularyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VocabularyDbContext>()
            .UseSqlite($"Data Source={_path}", sqlite => sqlite.MigrationsHistoryTable("__EFMigrationsHistory_Vocabulary"))
            .Options;
        return new VocabularyDbContext(options);
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
        return ValueTask.CompletedTask;
    }
}

public class VocabularyPersistenceTests
{
    private static IReadOnlyList<Lemma> BuildSeedLemmas()
    {
        // 与 data/seed/lemmas.json 同构的内存词元：名词（二词形）+ 动词（二词形）
        var coffee = Lemma.Create(
            Guid.NewGuid(), LanguageCode.Of("de-DE"), SemanticKey.Of("word.coffee"),
            PartOfSpeech.Noun, GermanArticle.Der, CefrLevel.A1,
            InflectionForm.Create(SemanticKey.Of("word.coffee"), new GrammaticalAnalysis[]
            {
                new(GrammaticalCase.Nominative, GrammaticalNumber.Singular, Gender: GrammaticalGender.Masculine),
                new(GrammaticalCase.Dative, GrammaticalNumber.Singular, Gender: GrammaticalGender.Masculine),
            }));
        coffee.AddTag("food");
        coffee.AddTag("daily");
        coffee.AddForm(SemanticKey.Of("word.coffee.form.plural"), new GrammaticalAnalysis[]
        {
            new(GrammaticalCase.Nominative, GrammaticalNumber.Plural),
            new(GrammaticalCase.Accusative, GrammaticalNumber.Plural),
        });

        var drink = Lemma.Create(
            Guid.NewGuid(), LanguageCode.Of("de-DE"), SemanticKey.Of("verb.drink"),
            PartOfSpeech.Verb, article: null, CefrLevel.A1,
            InflectionForm.Create(SemanticKey.Of("verb.drink"), new GrammaticalAnalysis[] { new(Tense: GrammaticalTense.Present) }));
        drink.AddForm(SemanticKey.Of("verb.drink.form.1sg"), new GrammaticalAnalysis[]
        {
            new(Person: GrammaticalPerson.First, Number: GrammaticalNumber.Singular, Tense: GrammaticalTense.Present),
        });

        return [coffee, drink];
    }

    [Fact]
    public async Task 词元词形语法分析与标签往返一致_形态顺序保留()
    {
        var seedLemmas = BuildSeedLemmas();
        await using var database = new TempVocabularyDatabase();

        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.Lemmas.AddRange(seedLemmas.Select(VocabularyPersistenceMapper.ToRow));
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var restored = (await read.Lemmas.OrderBy(l => l.LemmaKey).ToListAsync())
            .Select(VocabularyPersistenceMapper.ToDomain)
            .ToArray();

        var expected = seedLemmas.OrderBy(l => l.LemmaKey.Value).ToArray();
        restored.Should().BeEquivalentTo(expected,
            options => options.WithStrictOrderingFor(l => l.Forms).WithStrictOrderingFor(l => l.Tags));
    }

    [Fact]
    public async Task 原形词形由词元键识别_缺原形的库数据还原时失败()
    {
        var coffee = BuildSeedLemmas().Single(l => l.LemmaKey.Value == "word.coffee");
        await using var database = new TempVocabularyDatabase();

        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.Lemmas.Add(VocabularyPersistenceMapper.ToRow(coffee));
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var row = await read.Lemmas.SingleAsync(l => l.LemmaKey == "word.coffee");
        row.Forms.RemoveAll(f => f.SurfaceKey == "word.coffee"); // 破坏数据：抽掉原形词形

        var act = () => VocabularyPersistenceMapper.ToDomain(row);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task 词元键全局唯一()
    {
        var coffee = BuildSeedLemmas().Single(l => l.LemmaKey.Value == "word.coffee");
        await using var database = new TempVocabularyDatabase();

        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.Lemmas.Add(VocabularyPersistenceMapper.ToRow(coffee));
            await context.SaveChangesAsync();
        }

        // 同一 LemmaKey 不允许第二个词元（不同词义必须用语义键区分，见 model.md）
        await using var duplicate = database.CreateContext();
        var clone = Lemma.Create(
            Guid.NewGuid(), LanguageCode.Of("de-DE"), SemanticKey.Of("word.coffee"),
            PartOfSpeech.Noun, GermanArticle.Der, CefrLevel.A1,
            InflectionForm.Create(SemanticKey.Of("word.coffee"), new GrammaticalAnalysis[] { new() }));
        duplicate.Lemmas.Add(VocabularyPersistenceMapper.ToRow(clone));

        var act = () => duplicate.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
