using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverPeriodStateEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignmentFinalizedAt",
                table: "report_periods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                table: "report_periods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAssignmentFinalized",
                table: "report_periods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                table: "report_periods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveDaysWorked",
                table: "drivers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "CurrentWeekHoursWorked",
                table: "drivers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentWeekStartDate",
                table: "drivers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRestDay",
                table: "drivers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTripEndTime",
                table: "drivers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "driver_period_states",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    ReportPeriodId = table.Column<int>(type: "integer", nullable: false),
                    LastTripEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastTripEndLocation = table.Column<string>(type: "text", nullable: true),
                    IncompleteWeekHoursWorked = table.Column<double>(type: "double precision", nullable: false),
                    IncompleteWeekStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsecutiveDaysWorked = table.Column<int>(type: "integer", nullable: false),
                    LastRestDay = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalPeriodHoursWorked = table.Column<double>(type: "double precision", nullable: false),
                    TotalPeriodTrips = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_period_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_driver_period_states_drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_driver_period_states_report_periods_ReportPeriodId",
                        column: x => x.ReportPeriodId,
                        principalTable: "report_periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_driver_period_states_DriverId",
                table: "driver_period_states",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_driver_period_states_DriverId_ReportPeriodId",
                table: "driver_period_states",
                columns: new[] { "DriverId", "ReportPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_driver_period_states_ReportPeriodId",
                table: "driver_period_states",
                column: "ReportPeriodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "driver_period_states");

            migrationBuilder.DropColumn(
                name: "AssignmentFinalizedAt",
                table: "report_periods");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "report_periods");

            migrationBuilder.DropColumn(
                name: "IsAssignmentFinalized",
                table: "report_periods");

            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "report_periods");

            migrationBuilder.DropColumn(
                name: "ConsecutiveDaysWorked",
                table: "drivers");

            migrationBuilder.DropColumn(
                name: "CurrentWeekHoursWorked",
                table: "drivers");

            migrationBuilder.DropColumn(
                name: "CurrentWeekStartDate",
                table: "drivers");

            migrationBuilder.DropColumn(
                name: "LastRestDay",
                table: "drivers");

            migrationBuilder.DropColumn(
                name: "LastTripEndTime",
                table: "drivers");
        }
    }
}
