namespace TransitAnalyticsAPI.Admin.Services;

public interface IGtfsUploadService
{
    Task<GtfsUploadJob> SaveUploadAsync(IFormFile archive, CancellationToken cancellationToken = default);

    Task<GtfsUploadResult> ImportFromWorkRootAsync(
        string workRoot,
        Func<string, CancellationToken, Task>? reportProgress = null,
        CancellationToken cancellationToken = default);
}
