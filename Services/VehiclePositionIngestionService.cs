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
}
