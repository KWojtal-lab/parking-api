using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parking.Api.Migrations
{
  public partial class AddUserLicensePlates_RemoveTowAway : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "TowAway",
          table: "ParkingSessions");

      migrationBuilder.CreateTable(
          name: "UserLicensePlates",
          columns: table => new
          {
            UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            PlateNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_UserLicensePlates", x => new { x.UserId, x.PlateNumber });
          });

      migrationBuilder.CreateIndex(
          name: "IX_UserLicensePlates_UserId",
          table: "UserLicensePlates",
          column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "UserLicensePlates");

      migrationBuilder.AddColumn<bool>(
          name: "TowAway",
          table: "ParkingSessions",
          type: "boolean",
          nullable: false,
          defaultValue: false);
    }
  }
}
