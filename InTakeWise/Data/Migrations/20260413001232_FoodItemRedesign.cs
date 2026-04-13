using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTakeWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class FoodItemRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "FoodItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_NormalizedName",
                table: "FoodItems",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FoodItems_NormalizedName",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "FoodItems");
        }
    }
}
