using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class AddBonusCalculationExceptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bonus_calculation_exceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyNameContains = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ServiceTypeId = table.Column<int>(type: "integer", nullable: false),
                    CalculationMethod = table.Column<int>(type: "integer", nullable: false),
                    vehicle_types = table.Column<string>(type: "text", nullable: false),
                    location_keywords = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bonus_calculation_exceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bonus_calculation_exceptions_service_types_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "service_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bonus_calculation_exceptions_ServiceTypeId",
                table: "bonus_calculation_exceptions",
                column: "ServiceTypeId");

            // Seed: Sierra Nevada Round Trips via Chirchik, any vehicle type => hourly (DurationBased = 1)
            migrationBuilder.Sql(@"
                INSERT INTO bonus_calculation_exceptions
                    (""Name"", ""IsActive"", ""CompanyNameContains"", ""ServiceTypeId"", ""CalculationMethod"",
                     vehicle_types, location_keywords, ""CreatedAt"", ""UpdatedAt"")
                SELECT 'Sierra Nevada - Chirchik', TRUE, 'Sierra Nevada', st.""Id"", 1,
                       '[]', '[""Chirchik"",""Chirchiq""]', NOW(), NOW()
                FROM service_types st
                WHERE LOWER(st.""Name"") = 'round trip'
                LIMIT 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bonus_calculation_exceptions");
        }
    }
}
