using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Services;

public class ActiveImportRunResolver : IActiveImportRunResolver
{
    private readonly AppDbContext _appDbContext;

    public ActiveImportRunResolver(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<long?> GetActiveImportRunIdAsync(CancellationToken cancellationToken = default)
    {
        return await _appDbContext.GtfsImportRuns
            .AsNoTracking()
            .Where(importRun => importRun.IsActive && importRun.Status == "completed")
            .OrderByDescending(importRun => importRun.CompletedAtUtc)
            .Select(importRun => (long?)importRun.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
