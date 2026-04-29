using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class SeparateFromAirportRailwayRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuantityFromStandardVehicleRate",
                table: "bonus_settings",
                newName: "QuantityFromRailwayStandardRate");

            migrationBuilder.RenameColumn(
                name: "QuantityFromPremiumVehicleRate",
                table: "bonus_settings",
                newName: "QuantityFromRailwayPremiumRate");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityFromAirportPremiumRate",
                table: "bonus_settings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityFromAirportStandardRate",
                table: "bonus_settings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityFromAirportPremiumRate",
                table: "bonus_settings");

            migrationBuilder.DropColumn(
                name: "QuantityFromAirportStandardRate",
                table: "bonus_settings");

            migrationBuilder.RenameColumn(
                name: "QuantityFromRailwayStandardRate",
                table: "bonus_settings",
                newName: "QuantityFromStandardVehicleRate");

            migrationBuilder.RenameColumn(
                name: "QuantityFromRailwayPremiumRate",
                table: "bonus_settings",
                newName: "QuantityFromPremiumVehicleRate");
        }
    }
}
