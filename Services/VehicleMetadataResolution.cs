using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Services;

public sealed class VehicleMetadataResolution
{
    public string? RouteId { get; init; }

    public GtfsTrip? Trip { get; init; }

    public GtfsRoute? Route { get; init; }
}
