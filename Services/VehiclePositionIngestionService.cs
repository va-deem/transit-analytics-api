using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Clients.AucklandTransport;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class VehiclePositionIngestionService : IVehiclePositionIngestionService
{
    private readonly IAucklandTransportClient _aucklandTransportClient;
    private readonly IVehiclePositionMapper _vehiclePositionMapper;
    private readonly AppDbContext _appDbContext;

    public VehiclePositionIngestionService(
        IAucklandTransportClient aucklandTransportClient,
        IVehiclePositionMapper vehiclePositionMapper,
        AppDbContext appDbContext)
    {
        _aucklandTransportClient = aucklandTransportClient;
        _vehiclePositionMapper = vehiclePositionMapper;
        _appDbContext = appDbContext;
    }

    public async Task<VehiclePositionIngestionResult> IngestAsync(CancellationToken cancellationToken = default)
    {
        var response = await _aucklandTransportClient.GetVehicleLocationsAsync(cancellationToken);

        if (response?.Response is null)
        {
            return new VehiclePositionIngestionResult
            {
                Status = response?.Status ?? "EMPTY_RESPONSE"
            };
        }

        var vehiclePositions = _vehiclePositionMapper.Map(response.Response.Entity);
        vehiclePositions = await FilterToSupportedVehicleTypesAsync(vehiclePositions, cancellationToken);

        _appDbContext.VehiclePositions.AddRange(vehiclePositions);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return new VehiclePositionIngestionResult
        {
            Status = response.Status,
            HeaderTimestamp = response.Response.Header?.Timestamp,
            TotalEntities = response.Response.Entity.Count,
            SavedVehiclePositions = vehiclePositions.Count
        };
    }

    private async Task<List<VehiclePosition>> FilterToSupportedVehicleTypesAsync(
        List<VehiclePosition> vehiclePositions,
        CancellationToken cancellationToken)
    {
        var activeImportRunId = await _appDbContext.GtfsImportRuns
            .AsNoTracking()
            .Where(importRun => importRun.IsActive && importRun.Status == "completed")
            .OrderByDescending(importRun => importRun.CompletedAtUtc)
            .Select(importRun => (long?)importRun.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!activeImportRunId.HasValue)
        {
            return [];
        }

        var tripIds = vehiclePositions
            .Where(position => string.IsNullOrWhiteSpace(position.TripId) == false)
            .Select(position => position.TripId!)
            .Distinct()
            .ToList();

        var trips = await _appDbContext.GtfsTrips
            .AsNoTracking()
            .Where(trip => trip.ImportRunId == activeImportRunId.Value && tripIds.Contains(trip.TripId))
            .ToListAsync(cancellationToken);

        var tripsByTripId = trips.ToDictionary(trip => trip.TripId, StringComparer.Ordinal);

        var routeIds = vehiclePositions
            .Select(position => position.RouteId)
            .Where(routeId => string.IsNullOrWhiteSpace(routeId) == false)
            .Select(routeId => routeId!)
            .Concat(trips.Select(trip => trip.RouteId))
            .Distinct()
            .ToList();

        var routes = await _appDbContext.GtfsRoutes
            .AsNoTracking()
            .Where(route => route.ImportRunId == activeImportRunId.Value && routeIds.Contains(route.RouteId))
            .ToListAsync(cancellationToken);

        var routesByRouteId = routes.ToDictionary(route => route.RouteId, StringComparer.Ordinal);

        return vehiclePositions
            .Where(position =>
            {
                tripsByTripId.TryGetValue(position.TripId ?? string.Empty, out var trip);
                var routeId = position.RouteId ?? trip?.RouteId;

                if (routeId is null || !routesByRouteId.TryGetValue(routeId, out var route))
                {
                    return false;
                }

                return string.IsNullOrWhiteSpace(VehicleTypeMapper.Map(route.RouteType)) == false;
            })
            .ToList();
    }
}
