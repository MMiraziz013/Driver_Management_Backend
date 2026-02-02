using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class FuelAndGasEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "FuelConsumptionPer100Km",
                table: "vehicles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FuelTankCapacity",
                table: "vehicles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "vehicles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "InitialFuelLevel",
                table: "vehicles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "gas_purchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportPeriodId = table.Column<int>(type: "integer", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LitersAmount = table.Column<double>(type: "double precision", nullable: false),
                    FuelType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AmountUzs = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedLiters = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gas_purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gas_purchases_report_periods_ReportPeriodId",
                        column: x => x.ReportPeriodId,
                        principalTable: "report_periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_fuel_allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GasPurchaseId = table.Column<int>(type: "integer", nullable: false),
                    VehicleId = table.Column<int>(type: "integer", nullable: false),
                    ReportPeriodId = table.Column<int>(type: "integer", nullable: false),
                    LitersAllocated = table.Column<double>(type: "double precision", nullable: false),
                    AllocationCostUzs = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_fuel_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_fuel_allocations_gas_purchases_GasPurchaseId",
                        column: x => x.GasPurchaseId,
                        principalTable: "gas_purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vehicle_fuel_allocations_report_periods_ReportPeriodId",
                        column: x => x.ReportPeriodId,
                        principalTable: "report_periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vehicle_fuel_allocations_trips_TripId",
                        column: x => x.TripId,
                        principalTable: "trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vehicle_fuel_allocations_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gas_purchases_FuelType",
                table: "gas_purchases",
                column: "FuelType");

            migrationBuilder.CreateIndex(
                name: "IX_gas_purchases_PurchaseDate",
                table: "gas_purchases",
                column: "PurchaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_gas_purchases_ReportPeriodId",
                table: "gas_purchases",
                column: "ReportPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fuel_allocations_GasPurchaseId",
                table: "vehicle_fuel_allocations",
                column: "GasPurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fuel_allocations_ReportPeriodId",
                table: "vehicle_fuel_allocations",
                column: "ReportPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fuel_allocations_TripId",
                table: "vehicle_fuel_allocations",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fuel_allocations_VehicleId",
                table: "vehicle_fuel_allocations",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fuel_allocations_VehicleId_ReportPeriodId",
                table: "vehicle_fuel_allocations",
                columns: new[] { "VehicleId", "ReportPeriodId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_fuel_allocations");

            migrationBuilder.DropTable(
                name: "gas_purchases");

            migrationBuilder.DropColumn(
                name: "FuelConsumptionPer100Km",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "FuelTankCapacity",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "InitialFuelLevel",
                table: "vehicles");
        }
    }
}
