using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Parking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VehicleType",
                table: "Vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateTable(
                name: "ParkingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstHourFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SecondHourFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    EveryNextHourFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MaxParkingSpots = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ParkingSettings",
                columns: new[] { "Id", "EveryNextHourFee", "FirstHourFee", "MaxParkingSpots", "SecondHourFee" },
                values: new object[] { 1, 6m, 2m, 50, 4m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingSettings");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleType",
                table: "Vehicles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
