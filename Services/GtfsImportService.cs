using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class GtfsImportService : IGtfsImportService
{
    private const int BatchSize = 5000;

    private readonly AppDbContext _appDbContext;

    public GtfsImportService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<GtfsImportResult> ImportRoutesAndTripsAsync(
        string gtfsDirectory,
        CancellationToken cancellationToken = default)
    {
        var feedInfoPath = Path.Combine(gtfsDirectory, "feed_info.txt");
        var routesPath = Path.Combine(gtfsDirectory, "routes.txt");
        var shapesPath = Path.Combine(gtfsDirectory, "shapes.txt");
        var stopsPath = Path.Combine(gtfsDirectory, "stops.txt");
        var stopTimesPath = Path.Combine(gtfsDirectory, "stop_times.txt");
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
            var stops = ReadStops(stopsPath, importRun.Id);
            var trips = ReadTrips(tripsPath, importRun.Id);

            _appDbContext.GtfsRoutes.AddRange(routes);
            _appDbContext.GtfsStops.AddRange(stops);
            _appDbContext.GtfsTrips.AddRange(trips);
            await _appDbContext.SaveChangesAsync(cancellationToken);

            var importedShapePoints = await ImportShapePointsAsync(shapesPath, importRun.Id, cancellationToken);
            var importedStopTimes = await ImportStopTimesAsync(stopTimesPath, importRun.Id, cancellationToken);

            var activeRuns = await _appDbContext.GtfsImportRuns
                .Where(run => run.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var activeRun in activeRuns)
            {
                activeRun.IsActive = false;
            }

            var importRunToFinalize = await _appDbContext.GtfsImportRuns
                .SingleAsync(run => run.Id == importRun.Id, cancellationToken);

            importRunToFinalize.IsActive = true;
            importRunToFinalize.Status = "completed";
            importRunToFinalize.CompletedAtUtc = DateTime.UtcNow;

            await _appDbContext.SaveChangesAsync(cancellationToken);
            await DeleteInactiveImportRunsAsync(importRun.Id, cancellationToken);

            return new GtfsImportResult
            {
                SourceVersion = sourceVersion,
                ImportedRoutes = routes.Count,
                ImportedShapePoints = importedShapePoints,
                ImportedStops = stops.Count,
                ImportedStopTimes = importedStopTimes,
                ImportedTrips = trips.Count,
                ImportRunId = importRun.Id
            };
        }
        catch (Exception exception)
        {
            var importRunToFail = await _appDbContext.GtfsImportRuns
                .SingleAsync(run => run.Id == importRun.Id, cancellationToken);

            importRunToFail.Status = "failed";
            importRunToFail.Notes = exception.Message;
            importRunToFail.CompletedAtUtc = DateTime.UtcNow;
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

    private async Task<int> ImportShapePointsAsync(
        string filePath,
        long importRunId,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var batch = new List<GtfsShapePoint>(BatchSize);
        var importedCount = 0;

        foreach (var shapePoint in csv.GetRecords<GtfsShapePointRecord>())
        {
            batch.Add(new GtfsShapePoint
            {
                ImportRunId = importRunId,
                ShapeId = GetRequiredValue(shapePoint.ShapeId, "shape_id"),
                Latitude = shapePoint.ShapePointLatitude,
                Longitude = shapePoint.ShapePointLongitude,
                Sequence = shapePoint.ShapePointSequence,
                DistanceTraveled = shapePoint.ShapeDistanceTraveled
            });

            if (batch.Count >= BatchSize)
            {
                importedCount += await SaveBatchAsync(_appDbContext.GtfsShapePoints, batch, cancellationToken);
            }
        }

        if (batch.Count > 0)
        {
            importedCount += await SaveBatchAsync(_appDbContext.GtfsShapePoints, batch, cancellationToken);
        }

        return importedCount;
    }

    private static List<GtfsStop> ReadStops(string filePath, long importRunId)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        return csv.GetRecords<GtfsStopRecord>()
            .Select(stop => new GtfsStop
            {
                ImportRunId = importRunId,
                StopId = GetRequiredValue(stop.StopId, "stop_id"),
                StopCode = GetOptionalValue(stop.StopCode),
                StopName = GetOptionalValue(stop.StopName),
                StopLat = stop.StopLat,
                StopLon = stop.StopLon,
                LocationType = stop.LocationType,
                ParentStation = GetOptionalValue(stop.ParentStation),
                PlatformCode = GetOptionalValue(stop.PlatformCode)
            })
            .ToList();
    }

    private async Task<int> ImportStopTimesAsync(
        string filePath,
        long importRunId,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var batch = new List<GtfsStopTime>(BatchSize);
        var importedCount = 0;

        foreach (var stopTime in csv.GetRecords<GtfsStopTimeRecord>())
        {
            batch.Add(new GtfsStopTime
            {
                ImportRunId = importRunId,
                TripId = GetRequiredValue(stopTime.TripId, "trip_id"),
                StopId = GetRequiredValue(stopTime.StopId, "stop_id"),
                StopSequence = stopTime.StopSequence,
                StopHeadsign = GetOptionalValue(stopTime.StopHeadsign),
                ShapeDistTraveled = stopTime.ShapeDistTraveled
            });

            if (batch.Count >= BatchSize)
            {
                importedCount += await SaveBatchAsync(_appDbContext.GtfsStopTimes, batch, cancellationToken);
            }
        }

        if (batch.Count > 0)
        {
            importedCount += await SaveBatchAsync(_appDbContext.GtfsStopTimes, batch, cancellationToken);
        }

        return importedCount;
    }

    private async Task DeleteInactiveImportRunsAsync(long activeImportRunId, CancellationToken cancellationToken)
    {
        var inactiveImportRunIds = await _appDbContext.GtfsImportRuns
            .Where(run => !run.IsActive && run.Id != activeImportRunId)
            .Select(run => run.Id)
            .ToListAsync(cancellationToken);

        if (inactiveImportRunIds.Count == 0)
        {
            return;
        }

        await _appDbContext.GtfsRoutes
            .Where(route => inactiveImportRunIds.Contains(route.ImportRunId))
            .ExecuteDeleteAsync(cancellationToken);

        await _appDbContext.GtfsShapePoints
            .Where(shapePoint => inactiveImportRunIds.Contains(shapePoint.ImportRunId))
            .ExecuteDeleteAsync(cancellationToken);

        await _appDbContext.GtfsStops
            .Where(stop => inactiveImportRunIds.Contains(stop.ImportRunId))
            .ExecuteDeleteAsync(cancellationToken);

        await _appDbContext.GtfsStopTimes
            .Where(stopTime => inactiveImportRunIds.Contains(stopTime.ImportRunId))
            .ExecuteDeleteAsync(cancellationToken);

        await _appDbContext.GtfsTrips
            .Where(trip => inactiveImportRunIds.Contains(trip.ImportRunId))
            .ExecuteDeleteAsync(cancellationToken);

        await _appDbContext.GtfsImportRuns
            .Where(run => inactiveImportRunIds.Contains(run.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<int> SaveBatchAsync<TEntity>(
        DbSet<TEntity> dbSet,
        List<TEntity> batch,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        dbSet.AddRange(batch);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        var count = batch.Count;
        batch.Clear();
        _appDbContext.ChangeTracker.Clear();
        return count;
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

    private sealed class GtfsShapePointRecord
    {
        [Name("shape_id")]
        public string ShapeId { get; set; } = string.Empty;

        [Name("shape_pt_lat")]
        public double ShapePointLatitude { get; set; }

        [Name("shape_pt_lon")]
        public double ShapePointLongitude { get; set; }

        [Name("shape_pt_sequence")]
        public int ShapePointSequence { get; set; }

        [Name("shape_dist_traveled")]
        public double? ShapeDistanceTraveled { get; set; }
    }

    private sealed class GtfsStopRecord
    {
        [Name("stop_id")]
        public string StopId { get; set; } = string.Empty;

        [Name("stop_code")]
        public string? StopCode { get; set; }

        [Name("stop_name")]
        public string? StopName { get; set; }

        [Name("stop_lat")]
        public double StopLat { get; set; }

        [Name("stop_lon")]
        public double StopLon { get; set; }

        [Name("location_type")]
        public int? LocationType { get; set; }

        [Name("parent_station")]
        public string? ParentStation { get; set; }

        [Name("platform_code")]
        public string? PlatformCode { get; set; }
    }

    private sealed class GtfsStopTimeRecord
    {
        [Name("trip_id")]
        public string TripId { get; set; } = string.Empty;

        [Name("stop_id")]
        public string StopId { get; set; } = string.Empty;

        [Name("stop_sequence")]
        public int StopSequence { get; set; }

        [Name("stop_headsign")]
        public string? StopHeadsign { get; set; }

        [Name("shape_dist_traveled")]
        public double? ShapeDistTraveled { get; set; }
    }
}
