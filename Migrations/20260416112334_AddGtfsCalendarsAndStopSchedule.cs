using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransitAnalyticsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGtfsCalendarsAndStopSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_gtfs_stop_times_import_run_id",
                table: "gtfs_stop_times");

            migrationBuilder.AddColumn<int>(
                name: "arrival_time_seconds",
                table: "gtfs_stop_times",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "departure_time_seconds",
                table: "gtfs_stop_times",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "gtfs_calendar_dates",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_run_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    exception_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_calendar_dates", x => x.id);
                    table.ForeignKey(
                        name: "fk_gtfs_calendar_dates_gtfs_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "gtfs_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gtfs_calendars",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    import_run_id = table.Column<long>(type: "bigint", nullable: false),
                    service_id = table.Column<string>(type: "text", nullable: false),
                    monday = table.Column<bool>(type: "boolean", nullable: false),
                    tuesday = table.Column<bool>(type: "boolean", nullable: false),
                    wednesday = table.Column<bool>(type: "boolean", nullable: false),
                    thursday = table.Column<bool>(type: "boolean", nullable: false),
                    friday = table.Column<bool>(type: "boolean", nullable: false),
                    saturday = table.Column<bool>(type: "boolean", nullable: false),
                    sunday = table.Column<bool>(type: "boolean", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gtfs_calendars", x => x.id);
                    table.ForeignKey(
                        name: "fk_gtfs_calendars_gtfs_import_runs_import_run_id",
                        column: x => x.import_run_id,
                        principalTable: "gtfs_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_stop_times_import_run_id_stop_id_departure_time_seconds",
                table: "gtfs_stop_times",
                columns: new[] { "import_run_id", "stop_id", "departure_time_seconds" });

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_stop_times_import_run_id_trip_id_stop_sequence",
                table: "gtfs_stop_times",
                columns: new[] { "import_run_id", "trip_id", "stop_sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_calendar_dates_import_run_id_date_service_id",
                table: "gtfs_calendar_dates",
                columns: new[] { "import_run_id", "date", "service_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_calendars_import_run_id_service_id",
                table: "gtfs_calendars",
                columns: new[] { "import_run_id", "service_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gtfs_calendar_dates");

            migrationBuilder.DropTable(
                name: "gtfs_calendars");

            migrationBuilder.DropIndex(
                name: "ix_gtfs_stop_times_import_run_id_stop_id_departure_time_seconds",
                table: "gtfs_stop_times");

            migrationBuilder.DropIndex(
                name: "ix_gtfs_stop_times_import_run_id_trip_id_stop_sequence",
                table: "gtfs_stop_times");

            migrationBuilder.DropColumn(
                name: "arrival_time_seconds",
                table: "gtfs_stop_times");

            migrationBuilder.DropColumn(
                name: "departure_time_seconds",
                table: "gtfs_stop_times");

            migrationBuilder.CreateIndex(
                name: "ix_gtfs_stop_times_import_run_id",
                table: "gtfs_stop_times",
                column: "import_run_id");
        }
    }
}
