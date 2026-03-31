using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransitAnalyticsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGtfsRoutesAndTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gtfs_import_runs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_version = table.Column<string>(type: "text", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_import_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_routes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_run_id = table.Column<long>(type: "bigint", nullable: false),
                    route_id = table.Column<string>(type: "text", nullable: false),
                    agency_id = table.Column<string>(type: "text", nullable: true),
                    route_short_name = table.Column<string>(type: "text", nullable: true),
                    route_long_name = table.Column<string>(type: "text", nullable: true),
                    route_type = table.Column<int>(type: "integer", nullable: true),
                    route_color = table.Column<string>(type: "text", nullable: true),
                    route_text_color = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_routes", x => x.id);
                    table.ForeignKey(
                        name: "fk_gtfs_routes_gtfs_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "gtfs_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_trips",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_run_id = table.Column<long>(type: "bigint", nullable: false),
                    trip_id = table.Column<string>(type: "text", nullable: false),
                    route_id = table.Column<string>(type: "text", nullable: false),
                    service_id = table.Column<string>(type: "text", nullable: true),
                    trip_headsign = table.Column<string>(type: "text", nullable: true),
                    direction_id = table.Column<int>(type: "integer", nullable: true),
                    shape_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_trips", x => x.id);
                    table.ForeignKey(
                        name: "fk_gtfs_trips_gtfs_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "gtfs_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_routes_import_run_id",
                table: "gtfs_routes",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_trips_import_run_id",
                table: "gtfs_trips",
                column: "import_run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gtfs_routes");

            migrationBuilder.DropTable(
                name: "gtfs_trips");

            migrationBuilder.DropTable(
                name: "gtfs_import_runs");
        }
    }
}
