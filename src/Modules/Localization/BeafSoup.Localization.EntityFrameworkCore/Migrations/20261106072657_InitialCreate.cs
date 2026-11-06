using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeafSoup.Localization.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "localized_texts",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    LemmaKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localized_texts", x => new { x.Key, x.LanguageCode });
                });

            migrationBuilder.CreateTable(
                name: "semantic_keys",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_semantic_keys", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "voice_role_configs",
                columns: table => new
                {
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    AssetFileToken = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TtsVoiceName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voice_role_configs", x => new { x.LanguageCode, x.Role });
                });

            migrationBuilder.CreateIndex(
                name: "IX_localized_texts_LemmaKey",
                table: "localized_texts",
                column: "LemmaKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "localized_texts");

            migrationBuilder.DropTable(
                name: "semantic_keys");

            migrationBuilder.DropTable(
                name: "voice_role_configs");
        }
    }
}
