using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeafSoup.Identity.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AccountLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAtUtc",
                table: "user_accounts",
                type: "TEXT",
                nullable: true);

            // 兼容存量账户（ADR-008）：迁移前已注册的账户视为已验证，避免启用验证要求后锁定存量用户
            migrationBuilder.Sql(
                "UPDATE user_accounts SET EmailVerifiedAtUtc = RegisteredAtUtc WHERE EmailVerifiedAtUtc IS NULL;");

            migrationBuilder.CreateTable(
                name: "account_tokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Salt = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailedAttempts = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_tokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_tokens_ExpiresAtUtc",
                table: "account_tokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_account_tokens_TokenHash",
                table: "account_tokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_account_tokens_UserId_Purpose",
                table: "account_tokens",
                columns: new[] { "UserId", "Purpose" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_tokens");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAtUtc",
                table: "user_accounts");
        }
    }
}
