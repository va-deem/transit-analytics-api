using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class GtfsImportService : IGtfsImportService
{
    private readonly AppDbContext _appDbContext;
    private readonly IWebHostEnvironment _environment;

    public GtfsImportService(AppDbContext appDbContext, IWebHostEnvironment environment)
    {
        _appDbContext = appDbContext;
        _environment = environment;
    }

    public async Task<GtfsImportResult> ImportRoutesAndTripsAsync(CancellationToken cancellationToken = default)
    {
        var gtfsDirectory = Path.Combine(_environment.ContentRootPath, "data", "gtfs-static");
        var feedInfoPath = Path.Combine(gtfsDirectory, "feed_info.txt");
        var routesPath = Path.Combine(gtfsDirectory, "routes.txt");
        var tripsPath = Path.Combine(gtfsDirectory, "trips.txt");

        var sourceVersion = ReadFeedVersion(feedInfoPath);
        var importRun = new GtfsImportRun
        {
            SourceVersion = sourceVersion,
            StartedAtUtc = DateTime.UtcNow,
            Status = "running",
            IsActive = false
        };

        _appDbContext.GtfsImportRuns.Add(importRun);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var routes = ReadRoutes(routesPath, importRun.Id);
            var trips = ReadTrips(tripsPath, importRun.Id);

            _appDbContext.GtfsRoutes.AddRange(routes);
            _appDbContext.GtfsTrips.AddRange(trips);

            var activeRuns = await _appDbContext.GtfsImportRuns
                .Where(run => run.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var activeRun in activeRuns)
            {
                activeRun.IsActive = false;
            }

            importRun.IsActive = true;
            importRun.Status = "completed";
            importRun.CompletedAtUtc = DateTime.UtcNow;

            await _appDbContext.SaveChangesAsync(cancellationToken);

            return new GtfsImportResult
            {
                SourceVersion = sourceVersion,
                ImportedRoutes = routes.Count,
                ImportedTrips = trips.Count,
                ImportRunId = importRun.Id
            };
        }
        catch (Exception exception)
        {
            importRun.Status = "failed";
            importRun.Notes = exception.Message;
            importRun.CompletedAtUtc = DateTime.UtcNow;
            await _appDbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static string ReadFeedVersion(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var record = csv.GetRecords<GtfsFeedInfoRecord>().FirstOrDefault();

        if (record is null || string.IsNullOrWhiteSpace(record.FeedVersion))
        {
            throw new InvalidOperationException("GTFS feed_info.txt does not contain a valid feed_version.");
        }

        return record.FeedVersion;
    }

    private static List<GtfsRoute> ReadRoutes(string filePath, long importRunId)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        return csv.GetRecords<GtfsRouteRecord>()
            .Select(route => new GtfsRoute
            {
                ImportRunId = importRunId,
                RouteId = GetRequiredValue(route.RouteId, "route_id"),
                AgencyId = GetOptionalValue(route.AgencyId),
                RouteShortName = GetOptionalValue(route.RouteShortName),
                RouteLongName = GetOptionalValue(route.RouteLongName),
                RouteType = route.RouteType,
                RouteColor = GetOptionalValue(route.RouteColor),
                RouteTextColor = GetOptionalValue(route.RouteTextColor)
            })
            .ToList();
    }

    private static List<GtfsTrip> ReadTrips(string filePath, long importRunId)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        return csv.GetRecords<GtfsTripRecord>()
            .Select(trip => new GtfsTrip
            {
                ImportRunId = importRunId,
                TripId = GetRequiredValue(trip.TripId, "trip_id"),
                RouteId = GetRequiredValue(trip.RouteId, "route_id"),
                ServiceId = GetOptionalValue(trip.ServiceId),
                TripHeadsign = GetOptionalValue(trip.TripHeadsign),
                DirectionId = trip.DirectionId,
                ShapeId = GetOptionalValue(trip.ShapeId)
            })
            .ToList();
    }

    private static string GetRequiredValue(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required GTFS field '{fieldName}' is missing.");
        }

        return value;
    }

    private static string? GetOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private sealed class GtfsFeedInfoRecord
    {
        [Name("feed_version")]
        public string FeedVersion { get; set; } = string.Empty;
    }

    private sealed class GtfsRouteRecord
    {
        [Name("route_id")]
        public string RouteId { get; set; } = string.Empty;

        [Name("agency_id")]
        public string? AgencyId { get; set; }

        [Name("route_short_name")]
        public string? RouteShortName { get; set; }

        [Name("route_long_name")]
        public string? RouteLongName { get; set; }

        [Name("route_type")]
        public int? RouteType { get; set; }

        [Name("route_color")]
        public string? RouteColor { get; set; }

        [Name("route_text_color")]
        public string? RouteTextColor { get; set; }
    }

    private sealed class GtfsTripRecord
    {
        [Name("route_id")]
        public string RouteId { get; set; } = string.Empty;

        [Name("service_id")]
        public string? ServiceId { get; set; }

        [Name("trip_id")]
        public string TripId { get; set; } = string.Empty;

        [Name("trip_headsign")]
        public string? TripHeadsign { get; set; }

        [Name("direction_id")]
        public int? DirectionId { get; set; }

        [Name("shape_id")]
        public string? ShapeId { get; set; }
    }
}
