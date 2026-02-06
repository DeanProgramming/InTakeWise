using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTakeWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsersInformation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProfileUserName = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    WeightInKg = table.Column<int>(type: "int", nullable: false),
                    EveryDayFitnessLevel = table.Column<int>(type: "int", nullable: false),
                    ChosenGymDays = table.Column<int>(type: "int", nullable: false),
                    CaloriesTargetGymDay = table.Column<int>(type: "int", nullable: false),
                    ProteinTargetGymDay = table.Column<int>(type: "int", nullable: false),
                    CarbsTargetGymDay = table.Column<int>(type: "int", nullable: false),
                    FatTargetGymDay = table.Column<int>(type: "int", nullable: false),
                    FiberTargetGymDay = table.Column<int>(type: "int", nullable: false),
                    CaloriesTargetNonGymDay = table.Column<int>(type: "int", nullable: false),
                    ProteinTargetNonGymDay = table.Column<int>(type: "int", nullable: false),
                    CarbsTargetNonGymDay = table.Column<int>(type: "int", nullable: false),
                    FatTargetNonGymDay = table.Column<int>(type: "int", nullable: false),
                    FiberTargetNonGymDay = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersInformation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsersInformation_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsersInformation_UserId",
                table: "UsersInformation",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsersInformation");
        }
    }
}
