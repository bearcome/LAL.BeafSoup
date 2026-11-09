using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeafSoup.Identity.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class IdentityInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsTest = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_credentials",
                columns: table => new
                {
                    Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ProviderUserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_credentials", x => new { x.Provider, x.ProviderUserId });
                });

            migrationBuilder.CreateTable(
                name: "user_identity_links",
                columns: table => new
                {
                    Provider = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ProviderUserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_identity_links", x => new { x.Provider, x.ProviderUserId });
                });

            migrationBuilder.CreateTable(
                name: "user_registered_events",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsTest = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_registered_events", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastHeartbeatAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AccumulatedOnlineSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.SessionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_Status",
                table: "user_accounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_identity_links_UserId",
                table: "user_identity_links",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_LastHeartbeatAtUtc",
                table: "user_sessions",
                column: "LastHeartbeatAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_UserId",
                table: "user_sessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_accounts");

            migrationBuilder.DropTable(
                name: "user_credentials");

            migrationBuilder.DropTable(
                name: "user_identity_links");

            migrationBuilder.DropTable(
                name: "user_registered_events");

            migrationBuilder.DropTable(
                name: "user_sessions");
        }
    }
}
