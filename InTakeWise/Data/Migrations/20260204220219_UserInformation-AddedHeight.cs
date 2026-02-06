using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTakeWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserInformationAddedHeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HeightInCM",
                table: "UsersInformation",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeightInCM",
                table: "UsersInformation");
        }
    }
}
