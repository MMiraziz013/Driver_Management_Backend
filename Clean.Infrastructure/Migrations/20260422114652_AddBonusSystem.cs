using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class AddBonusSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bonus_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    QuantityPremiumVehicleRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QuantityStandardVehicleRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RoundTripPremiumVehicleRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RoundTripStandardVehicleRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DurationUnder2HoursRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DurationUnder4HoursRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Duration4To6HoursRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Duration6To12HoursRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DurationOver12HoursRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FieldTripDailyRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    premium_vehicle_types = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bonus_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "service_type_bonus_configs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceTypeId = table.Column<int>(type: "integer", nullable: false),
                    CalculationMethod = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_type_bonus_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_type_bonus_configs_service_types_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "service_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_type_bonus_configs_ServiceTypeId",
                table: "service_type_bonus_configs",
                column: "ServiceTypeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bonus_settings");

            migrationBuilder.DropTable(
                name: "service_type_bonus_configs");
        }
    }
}
