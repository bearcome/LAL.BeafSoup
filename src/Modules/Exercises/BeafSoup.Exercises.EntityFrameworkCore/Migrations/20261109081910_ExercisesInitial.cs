using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeafSoup.Exercises.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ExercisesInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercise_instances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    TypeKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PayloadVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    HintKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_instances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_instances_TypeKey",
                table: "exercise_instances",
                column: "TypeKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_instances");
        }
    }
}
