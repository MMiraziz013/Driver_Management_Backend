using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBonusSettingsRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Duration6To12HoursRate",
                table: "bonus_settings",
                newName: "QuantityFromStandardVehicleRate");

            migrationBuilder.AddColumn<decimal>(
                name: "Duration10To12HoursRate",
                table: "bonus_settings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Duration6To8HoursRate",
                table: "bonus_settings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Duration8To10HoursRate",
                table: "bonus_settings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityFromPremiumVehicleRate",
                table: "bonus_settings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration10To12HoursRate",
                table: "bonus_settings");

            migrationBuilder.DropColumn(
                name: "Duration6To8HoursRate",
                table: "bonus_settings");

            migrationBuilder.DropColumn(
                name: "Duration8To10HoursRate",
                table: "bonus_settings");

            migrationBuilder.DropColumn(
                name: "QuantityFromPremiumVehicleRate",
                table: "bonus_settings");

            migrationBuilder.RenameColumn(
                name: "QuantityFromStandardVehicleRate",
                table: "bonus_settings",
                newName: "Duration6To12HoursRate");
        }
    }
}
