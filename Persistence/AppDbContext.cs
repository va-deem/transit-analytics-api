using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<GtfsImportRun> GtfsImportRuns => Set<GtfsImportRun>();

    public DbSet<GtfsRoute> GtfsRoutes => Set<GtfsRoute>();

    public DbSet<GtfsTrip> GtfsTrips => Set<GtfsTrip>();

    public DbSet<VehiclePosition> VehiclePositions => Set<VehiclePosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GtfsImportRun>()
            .HasMany(importRun => importRun.Routes)
            .WithOne(route => route.ImportRun)
            .HasForeignKey(route => route.ImportRunId);

        modelBuilder.Entity<GtfsImportRun>()
            .HasMany(importRun => importRun.Trips)
            .WithOne(trip => trip.ImportRun)
            .HasForeignKey(trip => trip.ImportRunId);
    }
}
