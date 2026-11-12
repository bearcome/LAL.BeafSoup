using System.Text.Json;
using BeafSoup.Blazor.Shared.Components;
using FluentAssertions;
using Xunit;

namespace BeafSoup.Blazor.Tests;

/// <summary>背词形态表构表规则：动词按人称槽位入表，其他范式不混入表格。</summary>
public sealed class VocabularyFormsTableTests
{
    [Fact]
    public void Verb_paradigms_with_six_slots_become_table_rows_in_seed_order()
    {
        using var document = JsonDocument.Parse("""
        {
          "indicative_present": ["trinke", "trinkst", "trinkt", "trinken", "trinkt", "trinken"],
          "indicative_past": ["trank", "trankst", "trank", "tranken", "trankt", "tranken"],
          "indicative_perfect": ["habe getrunken", "hast getrunken", "hat getrunken", "haben getrunken", "habt getrunken", "haben getrunken"]
        }
        """);

        var table = VocabularyFormsTable.Build(document.RootElement, "Verb");

        table.HasRows.Should().BeTrue();
        table.Rows.Select(row => row.ParadigmKey).Should().Equal(
            "indicative_present", "indicative_past", "indicative_perfect");
        table.Rows[0].Cells.Should().Equal("trinke", "trinkst", "trinkt", "trinken", "trinkt", "trinken");
        table.Rows[0].Cells.Should().HaveCount(VocabularyFormsTable.PersonKeys.Count);
        table.Groups.Should().BeEmpty();
    }

    [Fact]
    public void Verb_rows_follow_canonical_tense_order_regardless_of_data_order()
    {
        using var document = JsonDocument.Parse("""
        {
          "indicative_perfect": ["habe getrunken", "hast getrunken", "hat getrunken", "haben getrunken", "habt getrunken", "haben getrunken"],
          "indicative_present": ["trinke", "trinkst", "trinkt", "trinken", "trinkt", "trinken"],
          "indicative_past": ["trank", "trankst", "trank", "tranken", "trankt", "tranken"]
        }
        """);

        var table = VocabularyFormsTable.Build(document.RootElement, "Verb");

        table.Rows.Select(row => row.ParadigmKey).Should().Equal(
            "indicative_present", "indicative_past", "indicative_perfect");
    }

    [Fact]
    public void Non_verb_paradigms_stay_grouped_instead_of_using_person_columns()
    {
        using var document = JsonDocument.Parse("""
        {
          "declension": ["Kaffee", "Kaffee", "Kaffee", "Kaffees", "Kaffees", "Kaffees", "Kaffees", "Kaffees"]
        }
        """);

        var table = VocabularyFormsTable.Build(document.RootElement, "Noun");

        table.HasRows.Should().BeFalse();
        table.Groups.Should().ContainSingle();
        table.Groups[0].ParadigmKey.Should().Be("declension");
        table.Groups[0].Cells.Should().HaveCount(8);
    }

    [Fact]
    public void Verb_paradigm_with_unexpected_slot_count_falls_back_to_group()
    {
        using var document = JsonDocument.Parse("""
        {
          "indicative_present": ["trinke", "trinkst", "trinkt"],
          "comparison": ["gut", "besser", "am besten"]
        }
        """);

        var table = VocabularyFormsTable.Build(document.RootElement, "Verb");

        table.HasRows.Should().BeFalse();
        table.Groups.Select(group => group.ParadigmKey).Should().Equal("indicative_present", "comparison");
    }

    [Fact]
    public void Missing_forms_object_produces_empty_table()
    {
        using var document = JsonDocument.Parse("{}");

        var table = VocabularyFormsTable.Build(document.RootElement, "Verb");

        table.HasRows.Should().BeFalse();
        table.Groups.Should().BeEmpty();
    }
}
