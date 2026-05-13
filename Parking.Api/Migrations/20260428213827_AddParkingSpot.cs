using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Parking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingSpot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxParkingSpots",
                table: "ParkingSettings");

            migrationBuilder.AddColumn<bool>(
                name: "TowAway",
                table: "ParkingSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ParkingSpots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsForDisabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSpots", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ParkingSpots",
                columns: new[] { "Id", "IsForDisabled" },
                values: new object[,]
                {
                    { 1, false },
                    { 2, false },
                    { 3, false },
                    { 4, false },
                    { 5, false },
                    { 6, false },
                    { 7, false },
                    { 8, false },
                    { 9, false },
                    { 10, false },
                    { 11, false },
                    { 12, false },
                    { 13, false },
                    { 14, false },
                    { 15, false },
                    { 16, false },
                    { 17, false },
                    { 18, false },
                    { 19, false },
                    { 20, false },
                    { 21, false },
                    { 22, false },
                    { 23, false },
                    { 24, false },
                    { 25, false },
                    { 26, false },
                    { 27, false },
                    { 28, false },
                    { 29, false },
                    { 30, false },
                    { 31, false },
                    { 32, false },
                    { 33, false },
                    { 34, false },
                    { 35, false },
                    { 36, false },
                    { 37, false },
                    { 38, false },
                    { 39, false },
                    { 40, false },
                    { 41, false },
                    { 42, false },
                    { 43, false },
                    { 44, false },
                    { 45, false },
                    { 46, false },
                    { 47, false },
                    { 48, false },
                    { 49, true },
                    { 50, true }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_ParkingSpots_SpotNumber",
                table: "ParkingSessions",
                column: "SpotNumber",
                principalTable: "ParkingSpots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_ParkingSpots_SpotNumber",
                table: "ParkingSessions");

            migrationBuilder.DropTable(
                name: "ParkingSpots");

            migrationBuilder.DropColumn(
                name: "TowAway",
                table: "ParkingSessions");

            migrationBuilder.AddColumn<int>(
                name: "MaxParkingSpots",
                table: "ParkingSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "ParkingSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "MaxParkingSpots",
                value: 50);
        }
    }
}
