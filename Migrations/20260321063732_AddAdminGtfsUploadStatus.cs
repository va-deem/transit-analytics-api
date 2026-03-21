using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitAnalyticsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminGtfsUploadStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "last_gtfs_import_error",
                table: "admin_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_gtfs_import_status",
                table: "admin_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_gtfs_source_version",
                table: "admin_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_gtfs_upload_at_utc",
                table: "admin_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_gtfs_upload_file_name",
                table: "admin_settings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_gtfs_import_error",
                table: "admin_settings");

            migrationBuilder.DropColumn(
                name: "last_gtfs_import_status",
                table: "admin_settings");

            migrationBuilder.DropColumn(
                name: "last_gtfs_source_version",
                table: "admin_settings");

            migrationBuilder.DropColumn(
                name: "last_gtfs_upload_at_utc",
                table: "admin_settings");

            migrationBuilder.DropColumn(
                name: "last_gtfs_upload_file_name",
                table: "admin_settings");
        }
    }
}
