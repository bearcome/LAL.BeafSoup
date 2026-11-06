using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeafSoup.Content.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TitleKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dialogues",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dialogues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "essays",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_essays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sentence_span_links",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SentenceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TextRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTokenIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    TokenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LexicalEntryKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sentence_span_links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sentences",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sentences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skill_units",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CourseId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_skill_units_courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dialogue_lines",
                columns: table => new
                {
                    DialogueId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    SpeakerLabel = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SentenceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    VoiceRole = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dialogue_lines", x => new { x.DialogueId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_dialogue_lines_dialogues_DialogueId",
                        column: x => x.DialogueId,
                        principalTable: "dialogues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "essay_sentences",
                columns: table => new
                {
                    EssayId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    SentenceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_essay_sentences", x => new { x.EssayId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_essay_sentences_essays_EssayId",
                        column: x => x.EssayId,
                        principalTable: "essays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sentence_tokenizations",
                columns: table => new
                {
                    SentenceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TextRevision = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sentence_tokenizations", x => new { x.SentenceId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_sentence_tokenizations_sentences_SentenceId",
                        column: x => x.SentenceId,
                        principalTable: "sentences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SkillUnitId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lessons_skill_units_SkillUnitId",
                        column: x => x.SkillUnitId,
                        principalTable: "skill_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sentence_tokens",
                columns: table => new
                {
                    SentenceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Start = table.Column<int>(type: "INTEGER", nullable: false),
                    Length = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    InflectionFormId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sentence_tokens", x => new { x.SentenceId, x.LanguageCode, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_sentence_tokens_sentence_tokenizations_SentenceId_LanguageCode",
                        columns: x => new { x.SentenceId, x.LanguageCode },
                        principalTable: "sentence_tokenizations",
                        principalColumns: new[] { "SentenceId", "LanguageCode" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lesson_exercises",
                columns: table => new
                {
                    LessonId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    ExerciseInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExerciseTypeKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lesson_exercises", x => new { x.LessonId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_lesson_exercises_lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lessons_SkillUnitId",
                table: "lessons",
                column: "SkillUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_sentence_span_links_SentenceId_LanguageCode_TextRevision",
                table: "sentence_span_links",
                columns: new[] { "SentenceId", "LanguageCode", "TextRevision" });

            migrationBuilder.CreateIndex(
                name: "IX_sentence_tokens_InflectionFormId",
                table: "sentence_tokens",
                column: "InflectionFormId");

            migrationBuilder.CreateIndex(
                name: "IX_skill_units_CourseId",
                table: "skill_units",
                column: "CourseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dialogue_lines");

            migrationBuilder.DropTable(
                name: "essay_sentences");

            migrationBuilder.DropTable(
                name: "lesson_exercises");

            migrationBuilder.DropTable(
                name: "sentence_span_links");

            migrationBuilder.DropTable(
                name: "sentence_tokens");

            migrationBuilder.DropTable(
                name: "dialogues");

            migrationBuilder.DropTable(
                name: "essays");

            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "sentence_tokenizations");

            migrationBuilder.DropTable(
                name: "skill_units");

            migrationBuilder.DropTable(
                name: "sentences");

            migrationBuilder.DropTable(
                name: "courses");
        }
    }
}
