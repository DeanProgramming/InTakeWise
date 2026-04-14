using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTakeWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class WeeklyMealSuggestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MealDateLocal",
                table: "ShoppingMealDays",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "WeekStartLocalDate",
                table: "ShoppingLists",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MealDateLocal",
                table: "ShoppingMealDays");

            migrationBuilder.DropColumn(
                name: "WeekStartLocalDate",
                table: "ShoppingLists");
        }
    }
}
