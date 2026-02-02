using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class AddReportPeriodStatusAndConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FuelFinalizedAt",
                table: "report_periods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFuelFinalized",
                table: "report_periods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "report_periods",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FuelFinalizedAt",
                table: "report_periods");

            migrationBuilder.DropColumn(
                name: "IsFuelFinalized",
                table: "report_periods");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "report_periods");
        }
    }
}
