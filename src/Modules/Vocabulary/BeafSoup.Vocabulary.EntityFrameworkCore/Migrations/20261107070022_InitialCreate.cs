using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeafSoup.Vocabulary.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lexical_concepts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConceptKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PartOfSpeech = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lexical_concepts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lexemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConceptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Lemma = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayWord = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    Register = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Article = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Cefr = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lexemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lexemes_lexical_concepts_ConceptId",
                        column: x => x.ConceptId,
                        principalTable: "lexical_concepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lexeme_tags",
                columns: table => new
                {
                    LexemeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lexeme_tags", x => new { x.LexemeId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_lexeme_tags_lexemes_LexemeId",
                        column: x => x.LexemeId,
                        principalTable: "lexemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "paradigm_cells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LexemeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParadigmKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SlotOrdinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Surface = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paradigm_cells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_paradigm_cells_lexemes_LexemeId",
                        column: x => x.LexemeId,
                        principalTable: "lexemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lexemes_ConceptId_LanguageCode_Ordinal",
                table: "lexemes",
                columns: new[] { "ConceptId", "LanguageCode", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lexical_concepts_ConceptKey",
                table: "lexical_concepts",
                column: "ConceptKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_paradigm_cells_LexemeId",
                table: "paradigm_cells",
                column: "LexemeId");

            migrationBuilder.CreateIndex(
                name: "IX_paradigm_cells_ParadigmKey",
                table: "paradigm_cells",
                column: "ParadigmKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lexeme_tags");

            migrationBuilder.DropTable(
                name: "paradigm_cells");

            migrationBuilder.DropTable(
                name: "lexemes");

            migrationBuilder.DropTable(
                name: "lexical_concepts");
        }
    }
}
