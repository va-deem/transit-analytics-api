using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class VehicleMetadataLookupService : IVehicleMetadataLookupService
{
    private readonly AppDbContext _appDbContext;
    private readonly IActiveImportRunResolver _activeImportRunResolver;

    public VehicleMetadataLookupService(
        AppDbContext appDbContext,
        IActiveImportRunResolver activeImportRunResolver)
    {
        _appDbContext = appDbContext;
        _activeImportRunResolver = activeImportRunResolver;
    }

    public async Task<VehicleMetadataLookup> BuildAsync(
        IEnumerable<VehicleMetadataKey> keys,
        CancellationToken cancellationToken = default)
    {
        var activeImportRunId = await _activeImportRunResolver.GetActiveImportRunIdAsync(cancellationToken);

        if (!activeImportRunId.HasValue)
        {
            return VehicleMetadataLookup.Empty;
        }

        var metadataKeys = keys.ToList();
        var tripIds = metadataKeys
            .Where(key => string.IsNullOrWhiteSpace(key.TripId) == false)
            .Select(key => key.TripId!)
            .Distinct()
            .ToList();

        var trips = await _appDbContext.GtfsTrips
            .AsNoTracking()
            .Where(trip => trip.ImportRunId == activeImportRunId.Value && tripIds.Contains(trip.TripId))
            .ToListAsync(cancellationToken);

        var tripsByTripId = trips.ToDictionary(trip => trip.TripId, StringComparer.Ordinal);

        var routeIds = metadataKeys
            .Select(key => key.RouteId)
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

        return new VehicleMetadataLookup(tripsByTripId, routesByRouteId);
    }
}
