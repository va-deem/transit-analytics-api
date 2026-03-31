using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AdminSettings> AdminSettings => Set<AdminSettings>();

    public DbSet<GtfsImportRun> GtfsImportRuns => Set<GtfsImportRun>();

    public DbSet<GtfsRoute> GtfsRoutes => Set<GtfsRoute>();

    public DbSet<GtfsShapePoint> GtfsShapePoints => Set<GtfsShapePoint>();

    public DbSet<GtfsStop> GtfsStops => Set<GtfsStop>();

    public DbSet<GtfsStopTime> GtfsStopTimes => Set<GtfsStopTime>();

    public DbSet<GtfsTrip> GtfsTrips => Set<GtfsTrip>();

    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    public DbSet<VehiclePosition> VehiclePositions => Set<VehiclePosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VehiclePosition>()
            .HasIndex(vehiclePosition => new { vehiclePosition.VehicleId, vehiclePosition.RecordedAtUtc });

        modelBuilder.Entity<VehiclePosition>()
            .HasIndex(vehiclePosition => vehiclePosition.RecordedAtUtc);

        modelBuilder.Entity<VehiclePosition>()
            .HasIndex(vehiclePosition => new { vehiclePosition.RouteId, vehiclePosition.RecordedAtUtc });

        modelBuilder.Entity<GtfsImportRun>()
            .HasMany(importRun => importRun.Routes)
            .WithOne(route => route.ImportRun)
            .HasForeignKey(route => route.ImportRunId);

        modelBuilder.Entity<GtfsImportRun>()
            .HasMany(importRun => importRun.ShapePoints)
            .WithOne(shapePoint => shapePoint.ImportRun)
            .HasForeignKey(shapePoint => shapePoint.ImportRunId);

        modelBuilder.Entity<GtfsImportRun>()
            .HasMany(importRun => importRun.Stops)
            .WithOne(stop => stop.ImportRun)
            .HasForeignKey(stop => stop.ImportRunId);

        modelBuilder.Entity<GtfsImportRun>()
            .HasMany(importRun => importRun.StopTimes)
            .WithOne(stopTime => stopTime.ImportRun)
            .HasForeignKey(stopTime => stopTime.ImportRunId);

        modelBuilder.Entity<GtfsImportRun>()
            .HasMany(importRun => importRun.Trips)
            .WithOne(trip => trip.ImportRun)
            .HasForeignKey(trip => trip.ImportRunId);
    }
}
