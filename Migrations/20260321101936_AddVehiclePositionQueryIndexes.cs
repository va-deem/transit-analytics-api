using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitAnalyticsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiclePositionQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_vehicle_positions_recorded_at_utc",
                table: "vehicle_positions",
                column: "recorded_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_positions_route_id_recorded_at_utc",
                table: "vehicle_positions",
                columns: new[] { "route_id", "recorded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_positions_vehicle_id_recorded_at_utc",
                table: "vehicle_positions",
                columns: new[] { "vehicle_id", "recorded_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vehicle_positions_recorded_at_utc",
                table: "vehicle_positions");

            migrationBuilder.DropIndex(
                name: "ix_vehicle_positions_route_id_recorded_at_utc",
                table: "vehicle_positions");

            migrationBuilder.DropIndex(
                name: "ix_vehicle_positions_vehicle_id_recorded_at_utc",
                table: "vehicle_positions");
        }
    }
}
