using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTakeWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandedLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivityType",
                table: "WorkoutLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "WorkoutLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Intensity",
                table: "WorkoutLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityType",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "Intensity",
                table: "WorkoutLogs");
        }
    }
}
