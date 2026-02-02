using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class ConfNumberToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfNumber",
                table: "trips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfNumber",
                table: "driver_assignments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfNumber",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "ConfNumber",
                table: "driver_assignments");
        }
    }
}
