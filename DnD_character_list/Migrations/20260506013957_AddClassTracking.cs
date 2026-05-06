using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DnD_character_list.Migrations
{
    /// <inheritdoc />
    public partial class AddClassTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pending_skill_choices",
                table: "Character",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pending_skill_count",
                table: "Character",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "primary_class_id",
                table: "Character",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "skills_pending",
                table: "Character",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pending_skill_choices",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "pending_skill_count",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "primary_class_id",
                table: "Character");

            migrationBuilder.DropColumn(
                name: "skills_pending",
                table: "Character");
        }
    }
}
