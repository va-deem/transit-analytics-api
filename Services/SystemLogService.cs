using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class SystemLogService<T> : ISystemLogService<T>
{
    private static readonly string Source = typeof(T).Name;

    private readonly AppDbContext _appDbContext;
    private readonly TimeProvider _timeProvider;

    public SystemLogService(AppDbContext appDbContext, TimeProvider timeProvider)
    {
        _appDbContext = appDbContext;
        _timeProvider = timeProvider;
    }

    public async Task LogAsync(SystemLogType type, string description, string? details = null,
        CancellationToken cancellationToken = default)
    {
        _appDbContext.SystemLogs.Add(new SystemLog
        {
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Type = type,
            Source = Source,
            Description = description,
            Details = details
        });

        await _appDbContext.SaveChangesAsync(cancellationToken);
    }
}
