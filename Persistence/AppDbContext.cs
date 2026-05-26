using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AdminSettings> AdminSettings => Set<AdminSettings>();

    public DbSet<FeedbackSubmission> FeedbackSubmissions => Set<FeedbackSubmission>();

    public DbSet<GtfsImportRun> GtfsImportRuns => Set<GtfsImportRun>();

    public DbSet<GtfsRoute> GtfsRoutes => Set<GtfsRoute>();

    public DbSet<GtfsCalendar> GtfsCalendars => Set<GtfsCalendar>();

    public DbSet<GtfsCalendarDate> GtfsCalendarDates => Set<GtfsCalendarDate>();

    public DbSet<GtfsShapePoint> GtfsShapePoints => Set<GtfsShapePoint>();

    public DbSet<GtfsStop> GtfsStops => Set<GtfsStop>();

    public DbSet<GtfsStopTime> GtfsStopTimes => Set<GtfsStopTime>();

    public DbSet<GtfsTrip> GtfsTrips => Set<GtfsTrip>();

    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    public DbSet<VehiclePosition> VehiclePositions => Set<VehiclePosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GtfsImportRun>()
            .Property(importRun => importRun.Status)
            .HasConversion(
                status => status.ToString().ToLowerInvariant(),
                value => Enum.Parse<GtfsImportStatus>(value, true));

        modelBuilder.Entity<VehiclePosition>()
            .HasIndex(vehiclePosition => new { vehiclePosition.VehicleId, vehiclePosition.RecordedAtUtc });

        modelBuilder.Entity<VehiclePosition>()
            .HasIndex(vehiclePosition => vehiclePosition.RecordedAtUtc);

        modelBuilder.Entity<VehiclePosition>()
            .HasIndex(vehiclePosition => new { vehiclePosition.RouteId, vehiclePosition.RecordedAtUtc });

        modelBuilder.Entity<FeedbackSubmission>()
            .HasIndex(feedbackSubmission => feedbackSubmission.CreatedAtUtc);

        modelBuilder.Entity<GtfsCalendar>()
            .HasIndex(calendar => new { calendar.ImportRunId, calendar.ServiceId });

        modelBuilder.Entity<GtfsCalendarDate>()
            .HasIndex(calendarDate => new { calendarDate.ImportRunId, calendarDate.Date, calendarDate.ServiceId });

        modelBuilder.Entity<GtfsStopTime>()
            .HasIndex(stopTime => new { stopTime.ImportRunId, stopTime.StopId, stopTime.DepartureTimeSeconds });

        modelBuilder.Entity<GtfsStopTime>()
            .HasIndex(stopTime => new { stopTime.ImportRunId, stopTime.TripId, stopTime.StopSequence });

        modelBuilder.Entity<GtfsImportRun>()
            .HasMany(importRun => importRun.Calendars)
            .WithOne(calendar => calendar.ImportRun)
            .HasForeignKey(calendar => calendar.ImportRunId);

        modelBuilder.Entity<GtfsImportRun>()
            .HasMany(importRun => importRun.CalendarDates)
            .WithOne(calendarDate => calendarDate.ImportRun)
            .HasForeignKey(calendarDate => calendarDate.ImportRunId);

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
