using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Admin.Services;

public interface IAdminSettingsService
{
    Task<AdminSettings> GetAsync(CancellationToken cancellationToken = default);

    Task<bool> IsMaintenanceModeEnabledAsync(CancellationToken cancellationToken = default);

    Task<bool> IsPollingEnabledAsync(CancellationToken cancellationToken = default);

    Task SetMaintenanceModeAsync(bool isEnabled, CancellationToken cancellationToken = default);

    Task SetPollingEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default);

    Task UpdateGtfsUploadStatusAsync(
        string fileName,
        string status,
        string? sourceVersion = null,
        string? error = null,
        CancellationToken cancellationToken = default);

    Task RecordGtfsUploadResultAsync(
        string fileName,
        bool isSuccessful,
        string? sourceVersion,
        string? error,
        CancellationToken cancellationToken = default);
}
