using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Admin.Services;

public class AdminSettingsService : IAdminSettingsService
{
    private const int SettingsRowId = 1;

    private readonly AppDbContext _appDbContext;
    private readonly IPollingRuntimeState _pollingRuntimeState;

    public AdminSettingsService(AppDbContext appDbContext, IPollingRuntimeState pollingRuntimeState)
    {
        _appDbContext = appDbContext;
        _pollingRuntimeState = pollingRuntimeState;
    }

    public async Task<AdminSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        return await GetOrCreateAsync(cancellationToken);
    }

    public async Task<bool> IsMaintenanceModeEnabledAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return settings.IsMaintenanceMode;
    }

    public async Task<bool> IsPollingEnabledAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _pollingRuntimeState.IsPollingEnabled;
    }

    public async Task SetMaintenanceModeAsync(bool isEnabled, CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        settings.IsMaintenanceMode = isEnabled;
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPollingEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        settings.IsPollingEnabled = isEnabled;
        await _appDbContext.SaveChangesAsync(cancellationToken);
        _pollingRuntimeState.SetPollingEnabled(isEnabled);
    }

    public async Task UpdateGtfsUploadStatusAsync(
        string fileName,
        string status,
        string? sourceVersion = null,
        string? error = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        settings.LastGtfsUploadAtUtc = DateTime.UtcNow;
        settings.LastGtfsUploadFileName = fileName;
        settings.LastGtfsImportStatus = status;
        settings.LastGtfsImportError = error;
        settings.LastGtfsSourceVersion = sourceVersion;
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordGtfsUploadResultAsync(
        string fileName,
        bool isSuccessful,
        string? sourceVersion,
        string? error,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        settings.LastGtfsUploadAtUtc = DateTime.UtcNow;
        settings.LastGtfsUploadFileName = fileName;
        settings.LastGtfsImportStatus = isSuccessful ? "completed" : "failed";
        settings.LastGtfsImportError = error;
        settings.LastGtfsSourceVersion = sourceVersion;
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdminSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await _appDbContext.AdminSettings
            .SingleOrDefaultAsync(item => item.Id == SettingsRowId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new AdminSettings
        {
            Id = SettingsRowId,
            IsMaintenanceMode = false,
            IsPollingEnabled = false
        };

        _appDbContext.AdminSettings.Add(settings);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
