using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Services;

public sealed class VehicleMetadataLookup
{
    private readonly Dictionary<string, GtfsTrip> _tripsByTripId;
    private readonly Dictionary<string, GtfsRoute> _routesByRouteId;

    public static VehicleMetadataLookup Empty { get; } = new([], []);

    public VehicleMetadataLookup(
        Dictionary<string, GtfsTrip> tripsByTripId,
        Dictionary<string, GtfsRoute> routesByRouteId)
    {
        _tripsByTripId = tripsByTripId;
        _routesByRouteId = routesByRouteId;
    }

    public VehicleMetadataResolution Resolve(string? tripId, string? routeId)
    {
        _tripsByTripId.TryGetValue(tripId ?? string.Empty, out var trip);

        var resolvedRouteId = routeId ?? trip?.RouteId;
        var route = resolvedRouteId is not null && _routesByRouteId.TryGetValue(resolvedRouteId, out var resolvedRoute)
            ? resolvedRoute
            : null;

        return new VehicleMetadataResolution
        {
            RouteId = resolvedRouteId,
            Trip = trip,
            Route = route
        };
    }
}
