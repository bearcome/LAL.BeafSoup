using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeafSoup.Learning.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class LearningInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "form_mastery",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CellId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    CorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IncorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_mastery", x => new { x.ProfileId, x.CellId });
                });

            migrationBuilder.CreateTable(
                name: "learner_profiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    BaseLanguage = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TargetLanguage = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TargetRetentionRate = table.Column<double>(type: "REAL", nullable: false),
                    ThemePreference = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    InflectionDisplay = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    InspectorTrigger = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learner_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learner_progress_states",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Hearts = table.Column<int>(type: "INTEGER", nullable: false),
                    LastHeartChangeAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivityDateUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DayStreak = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learner_progress_states", x => x.ProfileId);
                });

            migrationBuilder.CreateTable(
                name: "learning_activities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    DedupeKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProfileId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExerciseInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    LessonId = table.Column<string>(type: "TEXT", nullable: true),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_activities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "review_cards",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ConceptId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    DueUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Stability = table.Column<double>(type: "REAL", nullable: false),
                    Difficulty = table.Column<double>(type: "REAL", nullable: false),
                    SchedulingVersion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_cards", x => new { x.ProfileId, x.ConceptId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_learner_profiles_UserId",
                table: "learner_profiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_activities_ProfileId",
                table: "learning_activities",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_activities_ProfileId_DedupeKey",
                table: "learning_activities",
                columns: new[] { "ProfileId", "DedupeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_review_cards_DueUtc",
                table: "review_cards",
                column: "DueUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "form_mastery");

            migrationBuilder.DropTable(
                name: "learner_profiles");

            migrationBuilder.DropTable(
                name: "learner_progress_states");

            migrationBuilder.DropTable(
                name: "learning_activities");

            migrationBuilder.DropTable(
                name: "review_cards");
        }
    }
}
