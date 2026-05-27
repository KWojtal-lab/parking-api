using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLicensePlateVehicleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "UserLicensePlates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Sedan");

            migrationBuilder.Sql(
                "UPDATE \"UserLicensePlates\" SET \"VehicleType\" = 'Sedan' WHERE \"VehicleType\" = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "UserLicensePlates");
        }
    }
}
