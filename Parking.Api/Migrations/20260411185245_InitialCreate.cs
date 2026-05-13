using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parking.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    PlateNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VehicleType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.PlateNumber);
                });

            migrationBuilder.CreateTable(
                name: "ParkingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlateNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SpotNumber = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingSessions_Vehicles_PlateNumber",
                        column: x => x.PlateNumber,
                        principalTable: "Vehicles",
                        principalColumn: "PlateNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_EndTime",
                table: "ParkingSessions",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_PlateNumber",
                table: "ParkingSessions",
                column: "PlateNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_SpotNumber",
                table: "ParkingSessions",
                column: "SpotNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_UserId",
                table: "ParkingSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingSessions");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
