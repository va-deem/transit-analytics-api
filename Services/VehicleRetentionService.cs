using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class VehicleRetentionService : IVehicleRetentionService
{
    private readonly AppDbContext _appDbContext;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _historyRetention;
    private readonly ISystemLogService<VehicleRetentionService> _systemLog;

    public VehicleRetentionService(
        AppDbContext appDbContext,
        TimeProvider timeProvider,
        IOptions<VehicleOptions> vehicleOptions,
        ISystemLogService<VehicleRetentionService> systemLogService)
    {
        _appDbContext = appDbContext;
        _timeProvider = timeProvider;
        _historyRetention = TimeSpan.FromDays(Math.Max(1, vehicleOptions.Value.HistoryRetentionDays));
        _systemLog = systemLogService;
    }

    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoffUtc = _timeProvider.GetUtcNow().UtcDateTime - _historyRetention;

        var dbSize = await _appDbContext.Database
            .SqlQueryRaw<string>("SELECT pg_size_pretty(pg_database_size(current_database())) AS \"Value\"")
            .FirstAsync(cancellationToken);

        await _systemLog.LogAsync(SystemLogType.Info, "Started vehicle retention cleanup",
            $"Current db size: {dbSize}.", cancellationToken);

        return await _appDbContext.VehiclePositions
            .Where(vehiclePosition => vehiclePosition.RecordedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
