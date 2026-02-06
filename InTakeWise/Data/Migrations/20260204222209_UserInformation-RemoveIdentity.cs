using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTakeWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserInformationRemoveIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsersInformation_AspNetUsers_UserId",
                table: "UsersInformation");

            migrationBuilder.DropIndex(
                name: "IX_UsersInformation_UserId",
                table: "UsersInformation");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UsersInformation",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UsersInformation",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_UsersInformation_UserId",
                table: "UsersInformation",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsersInformation_AspNetUsers_UserId",
                table: "UsersInformation",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
