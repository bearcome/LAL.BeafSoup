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
                name: "lemmas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LemmaKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PartOfSpeech = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Article = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Cefr = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lemmas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inflection_forms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LemmaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    SurfaceKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inflection_forms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inflection_forms_lemmas_LemmaId",
                        column: x => x.LemmaId,
                        principalTable: "lemmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lemma_tags",
                columns: table => new
                {
                    LemmaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lemma_tags", x => new { x.LemmaId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_lemma_tags_lemmas_LemmaId",
                        column: x => x.LemmaId,
                        principalTable: "lemmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_analyses",
                columns: table => new
                {
                    FormId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Case = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Number = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Person = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Tense = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Degree = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_analyses", x => new { x.FormId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_form_analyses_inflection_forms_FormId",
                        column: x => x.FormId,
                        principalTable: "inflection_forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inflection_forms_LemmaId",
                table: "inflection_forms",
                column: "LemmaId");

            migrationBuilder.CreateIndex(
                name: "IX_inflection_forms_SurfaceKey",
                table: "inflection_forms",
                column: "SurfaceKey");

            migrationBuilder.CreateIndex(
                name: "IX_lemmas_LemmaKey",
                table: "lemmas",
                column: "LemmaKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_analyses");

            migrationBuilder.DropTable(
                name: "lemma_tags");

            migrationBuilder.DropTable(
                name: "inflection_forms");

            migrationBuilder.DropTable(
                name: "lemmas");
        }
    }
}
