using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDurationBrackets12To14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DurationOver12HoursRate",
                table: "bonus_settings",
                newName: "DurationOver14HoursRate");

            migrationBuilder.AddColumn<decimal>(
                name: "Duration12To14HoursRate",
                table: "bonus_settings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Duration12To14HoursRate",
                table: "bonus_settings");

            migrationBuilder.RenameColumn(
                name: "DurationOver14HoursRate",
                table: "bonus_settings",
                newName: "DurationOver12HoursRate");
        }
    }
}
