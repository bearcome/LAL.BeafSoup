using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeafSoup.Content.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenConceptKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConceptKey",
                table: "sentence_tokens",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sentence_tokens_ConceptKey",
                table: "sentence_tokens",
                column: "ConceptKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sentence_tokens_ConceptKey",
                table: "sentence_tokens");

            migrationBuilder.DropColumn(
                name: "ConceptKey",
                table: "sentence_tokens");
        }
    }
}
