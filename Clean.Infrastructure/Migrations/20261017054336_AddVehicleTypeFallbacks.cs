using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClassLibrary1.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTypeFallbacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_type_fallbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VehicleTypeId = table.Column<int>(type: "integer", nullable: false),
                    FallbackVehicleTypeId = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_type_fallbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_type_fallbacks_vehicle_types_FallbackVehicleTypeId",
                        column: x => x.FallbackVehicleTypeId,
                        principalTable: "vehicle_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicle_type_fallbacks_vehicle_types_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalTable: "vehicle_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_type_fallbacks_FallbackVehicleTypeId",
                table: "vehicle_type_fallbacks",
                column: "FallbackVehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_type_fallbacks_VehicleTypeId_FallbackVehicleTypeId",
                table: "vehicle_type_fallbacks",
                columns: new[] { "VehicleTypeId", "FallbackVehicleTypeId" },
                unique: true);

            // Initial chains, matched by name. Types that don't exist are skipped and can be set via the API.
            migrationBuilder.Sql(@"
                INSERT INTO vehicle_type_fallbacks (""VehicleTypeId"", ""FallbackVehicleTypeId"", ""Priority"")
                SELECT src.""Id"", dst.""Id"", chain.priority
                FROM (VALUES
                    ('suv', 'standard sedan', 1),
                    ('suv', 'hyundai sonata', 2),
                    ('suv', 'hyundai tucson', 3),
                    ('minivan', 'minibus', 1),
                    ('minivan', 'luxury minibus', 2),
                    ('luxury minibus', 'minibus', 1)
                ) AS chain(source_name, fallback_name, priority)
                JOIN vehicle_types src ON lower(trim(src.""Name"")) = chain.source_name
                JOIN vehicle_types dst ON lower(trim(dst.""Name"")) = chain.fallback_name
                ON CONFLICT (""VehicleTypeId"", ""FallbackVehicleTypeId"") DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_type_fallbacks");
        }
    }
}
