using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class VehicleRetentionService : IVehicleRetentionService
{
    private readonly AppDbContext _appDbContext;
    private readonly TimeSpan _historyRetention;

    public VehicleRetentionService(
        AppDbContext appDbContext,
        IOptions<VehicleOptions> vehicleOptions)
    {
        _appDbContext = appDbContext;
        _historyRetention = TimeSpan.FromDays(Math.Max(1, vehicleOptions.Value.HistoryRetentionDays));
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoffUtc = DateTime.UtcNow - _historyRetention;

        return await _appDbContext.VehiclePositions
            .Where(vehiclePosition => vehiclePosition.RecordedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
