using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoupleLoveSystem.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceBoard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "BoardMessages",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "BoardMessages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnlocked",
                table: "BoardMessages",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ReceiverUserId",
                table: "BoardMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "BoardMessages",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardMessages_ReceiverUserId",
                table: "BoardMessages",
                column: "ReceiverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMessages_ScheduledAt_IsUnlocked",
                table: "BoardMessages",
                columns: new[] { "ScheduledAt", "IsUnlocked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BoardMessages_ReceiverUserId",
                table: "BoardMessages");

            migrationBuilder.DropIndex(
                name: "IX_BoardMessages_ScheduledAt_IsUnlocked",
                table: "BoardMessages");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "BoardMessages");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "BoardMessages");

            migrationBuilder.DropColumn(
                name: "IsUnlocked",
                table: "BoardMessages");

            migrationBuilder.DropColumn(
                name: "ReceiverUserId",
                table: "BoardMessages");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "BoardMessages");
        }
    }
}
