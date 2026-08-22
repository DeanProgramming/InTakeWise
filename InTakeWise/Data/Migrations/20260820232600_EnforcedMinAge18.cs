using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InTakeWise.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforcedMinAge18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UsersInformation_ProfileRanges",
                table: "UsersInformation");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsersInformation_ProfileRanges",
                table: "UsersInformation",
                sql: "[Age] BETWEEN 18 AND 120 AND [HeightInCM] BETWEEN 120 AND 250 AND [WeightInKg] BETWEEN 30 AND 350");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UsersInformation_ProfileRanges",
                table: "UsersInformation");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsersInformation_ProfileRanges",
                table: "UsersInformation",
                sql: "[Age] BETWEEN 13 AND 120 AND [HeightInCM] BETWEEN 120 AND 250 AND [WeightInKg] BETWEEN 30 AND 350");
        }
    }
}
