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
    private static readonly LanguageCode De = LanguageCode.Of("de-DE");
    private static readonly LanguageCode Zh = LanguageCode.Of("zh-CN");

    // 与 data/seed/concepts.json 同构的内存概念：一词多语、一义多词元、范式槽位
    private static LexicalConcept BuildDrinkConcept()
    {
        var concept = new LexicalConcept(SemanticKey.Of("concept.drink"), PartOfSpeech.Verb, LexemeKind.Word);
        var trinken = concept.AddLexeme(De, "trinken", isPrimary: true, LexemeRegister.Neutral, article: null, CefrLevel.A1);
        trinken.AddTag("daily");
        var present = new[] { "trinke", "trinkst", "trinkt", "trinken", "trinkt", "trinken" };
        for (var slot = 0; slot < present.Length; slot++)
            trinken.AddParadigmCell(ParadigmCell.ComputeId("concept.drink", "de-DE", "trinken", "indicative_present", slot), "indicative_present", slot,
                present[slot]);
        concept.AddLexeme(De, "zechen", isPrimary: false, LexemeRegister.Elevated, article: null, CefrLevel.B2);
        concept.AddLexeme(Zh, "喝", isPrimary: true, LexemeRegister.Common, article: null, CefrLevel.A1);
        return concept;
    }

    [Fact]
    public async Task 概念词元范式槽位与标签往返一致_槽位顺序保留()
    {
        var seed = BuildDrinkConcept();
        await using var database = new TempVocabularyDatabase();

        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.LexicalConcepts.Add(VocabularyPersistenceMapper.ToRow(seed));
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var restored = VocabularyPersistenceMapper.ToDomain(
            await read.LexicalConcepts.SingleAsync(c => c.ConceptKey == "concept.drink"));

        restored.Should().BeEquivalentTo(seed,
            options => options.WithStrictOrderingFor(c => c.Lexemes));
        // 标签与范式槽位顺序保持（槽位序号即人称/数或格/数约定）
        foreach (var (expectedLexeme, restoredLexeme) in
                 seed.Lexemes.Zip(restored.Lexemes, (expected, actual) => (expected, actual)))
        {
            restoredLexeme.Tags.Should().Equal(expectedLexeme.Tags);
            restoredLexeme.Cells.Select(c => (c.ParadigmKey, c.SlotOrdinal, c.Surface, c.Id)).Should()
                .Equal(expectedLexeme.Cells.Select(c => (c.ParadigmKey, c.SlotOrdinal, c.Surface, c.Id)));
        }
        // Id 全部按领域规则确定性生成，跨读写稳定（形态掌握度与句子标注可安全引用）
        restored.Id.Should().Be(seed.Id);
        restored.Lexemes.Select(l => l.Id).Should().Equal(seed.Lexemes.Select(l => l.Id));
        restored.Lexemes.SelectMany(l => l.Cells).Select(c => c.Id)
            .Should().Equal(seed.Lexemes.SelectMany(l => l.Cells).Select(c => c.Id));
    }

    [Fact]
    public async Task 概念键全局唯一()
    {
        await using var database = new TempVocabularyDatabase();
        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.LexicalConcepts.Add(VocabularyPersistenceMapper.ToRow(BuildDrinkConcept()));
            await context.SaveChangesAsync();
        }

        // 同一 ConceptKey 不允许第二个概念（不同词义必须用不同语义键，见 model.md）
        await using var duplicate = database.CreateContext();
        var clone = new LexicalConcept(SemanticKey.Of("concept.drink"), PartOfSpeech.Verb, LexemeKind.Word);
        duplicate.LexicalConcepts.Add(VocabularyPersistenceMapper.ToRow(clone));

        var act = () => duplicate.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task 还原时主词元不变量仍由领域校验()
    {
        await using var database = new TempVocabularyDatabase();
        await using (var context = await database.CreateAndMigrateAsync())
        {
            context.LexicalConcepts.Add(VocabularyPersistenceMapper.ToRow(BuildDrinkConcept()));
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var row = await read.LexicalConcepts.SingleAsync(c => c.ConceptKey == "concept.drink");
        row.Lexemes.Single(l => l.Lemma == "喝").IsPrimary = false; // 破坏数据：抽掉中文主词元

        var act = () => VocabularyPersistenceMapper.ToDomain(row);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(VocabularyErrorCodes.MissingPrimaryLexeme);
    }
}
