using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parking.Auth.Migrations
{
  /// <inheritdoc />
  public partial class AddUserMoney : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<decimal>(
          name: "Money",
          table: "AspNetUsers",
          type: "numeric",
          nullable: false,
          defaultValue: 0m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "Money",
          table: "AspNetUsers");
    }
  }
}
