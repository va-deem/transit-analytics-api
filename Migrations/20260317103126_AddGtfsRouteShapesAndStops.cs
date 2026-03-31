using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransitAnalyticsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGtfsRouteShapesAndStops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gtfs_shape_points",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_run_id = table.Column<long>(type: "bigint", nullable: false),
                    shape_id = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    distance_traveled = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_shape_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_gtfs_shape_points_gtfs_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "gtfs_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_stop_times",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_run_id = table.Column<long>(type: "bigint", nullable: false),
                    trip_id = table.Column<string>(type: "text", nullable: false),
                    stop_id = table.Column<string>(type: "text", nullable: false),
                    stop_sequence = table.Column<int>(type: "integer", nullable: false),
                    stop_headsign = table.Column<string>(type: "text", nullable: true),
                    shape_dist_traveled = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_stop_times", x => x.id);
                    table.ForeignKey(
                        name: "fk_gtfs_stop_times_gtfs_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "gtfs_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_stops",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_run_id = table.Column<long>(type: "bigint", nullable: false),
                    stop_id = table.Column<string>(type: "text", nullable: false),
                    stop_code = table.Column<string>(type: "text", nullable: true),
                    stop_name = table.Column<string>(type: "text", nullable: true),
                    stop_lat = table.Column<double>(type: "double precision", nullable: false),
                    stop_lon = table.Column<double>(type: "double precision", nullable: false),
                    location_type = table.Column<int>(type: "integer", nullable: true),
                    parent_station = table.Column<string>(type: "text", nullable: true),
                    platform_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_stops", x => x.id);
                    table.ForeignKey(
                        name: "fk_gtfs_stops_gtfs_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "gtfs_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_shape_points_import_run_id",
                table: "gtfs_shape_points",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_stop_times_import_run_id",
                table: "gtfs_stop_times",
                column: "import_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_stops_import_run_id",
                table: "gtfs_stops",
                column: "import_run_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gtfs_shape_points");

            migrationBuilder.DropTable(
                name: "gtfs_stop_times");

            migrationBuilder.DropTable(
                name: "gtfs_stops");
        }
    }
}
