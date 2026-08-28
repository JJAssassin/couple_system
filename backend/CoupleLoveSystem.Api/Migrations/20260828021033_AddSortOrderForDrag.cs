using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoupleLoveSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSortOrderForDrag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Wishes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Todos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "CoupleId",
                table: "Settings",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_CoupleId",
                table: "Settings",
                column: "CoupleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Settings_CoupleId",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Wishes");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Todos");

            migrationBuilder.AlterColumn<string>(
                name: "CoupleId",
                table: "Settings",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
