using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TransitAnalyticsAPI.Admin.Services;

namespace TransitAnalyticsAPI.Areas.Admin.Pages;

[Authorize(Policy = "AdminOnly")]
public class SettingsModel : PageModel
{
    private readonly IAdminSettingsService _adminSettingsService;
    private readonly IGtfsUploadService _gtfsUploadService;
    private readonly IGtfsUploadQueue _gtfsUploadQueue;

    public SettingsModel(
        IAdminSettingsService adminSettingsService,
        IGtfsUploadService gtfsUploadService,
        IGtfsUploadQueue gtfsUploadQueue)
    {
        _adminSettingsService = adminSettingsService;
        _gtfsUploadService = gtfsUploadService;
        _gtfsUploadQueue = gtfsUploadQueue;
    }

    [BindProperty]
    public bool IsMaintenanceMode { get; set; }

    [BindProperty]
    public bool IsPollingEnabled { get; set; }

    [BindProperty]
    public IFormFile? GtfsArchive { get; set; }

    public DateTime? LastGtfsUploadAtUtc { get; private set; }

    public string? LastGtfsUploadFileName { get; private set; }

    public string? LastGtfsImportStatus { get; private set; }

    public string? LastGtfsImportError { get; private set; }

    public string? LastGtfsSourceVersion { get; private set; }

    public bool IsGtfsImportRunning =>
        LastGtfsImportStatus is "queued" or "extracting" or "validating" or "importing";

    public string GtfsProgressLabel => LastGtfsImportStatus switch
    {
        "queued" => "Queued",
        "extracting" => "Extracting archive",
        "validating" => "Validating files",
        "importing" => "Importing into database",
        "completed" => "Completed",
        "failed" => "Failed",
        _ => "Not available"
    };

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadSettingsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostToggleMaintenanceAsync(CancellationToken cancellationToken)
    {
        await _adminSettingsService.SetMaintenanceModeAsync(IsMaintenanceMode, cancellationToken);
        StatusMessage = IsMaintenanceMode
            ? "Maintenance mode enabled."
            : "Maintenance mode disabled.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTogglePollingAsync(CancellationToken cancellationToken)
    {
        await _adminSettingsService.SetPollingEnabledAsync(IsPollingEnabled, cancellationToken);
        StatusMessage = IsPollingEnabled
            ? "AT polling enabled."
            : "AT polling disabled.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadGtfsAsync(CancellationToken cancellationToken)
    {
        if (GtfsArchive is null)
        {
            StatusMessage = "Select a GTFS .zip archive before uploading.";
            await LoadSettingsAsync(cancellationToken);
            return Page();
        }

        try
        {
            var job = await _gtfsUploadService.SaveUploadAsync(GtfsArchive, cancellationToken);
            var queued = _gtfsUploadQueue.TryEnqueue(job);
            if (!queued)
            {
                if (Directory.Exists(job.WorkRoot))
                {
                    Directory.Delete(job.WorkRoot, recursive: true);
                }

                StatusMessage = "A GTFS import is already in progress. Try again after it completes.";
                await LoadSettingsAsync(cancellationToken);
                return Page();
            }

            await _adminSettingsService.UpdateGtfsUploadStatusAsync(
                GtfsArchive.FileName,
                "queued",
                cancellationToken: cancellationToken);
            StatusMessage = "GTFS upload accepted. Import is running in the background.";
            return RedirectToPage();
        }
        catch (Exception exception)
        {
            await _adminSettingsService.RecordGtfsUploadResultAsync(
                GtfsArchive.FileName,
                isSuccessful: false,
                sourceVersion: null,
                error: exception.Message,
                cancellationToken);

            StatusMessage = $"GTFS upload failed: {exception.Message}";
            await LoadSettingsAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }

    private async Task LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _adminSettingsService.GetAsync(cancellationToken);
        IsMaintenanceMode = settings.IsMaintenanceMode;
        IsPollingEnabled = settings.IsPollingEnabled;
        LastGtfsUploadAtUtc = settings.LastGtfsUploadAtUtc;
        LastGtfsUploadFileName = settings.LastGtfsUploadFileName;
        LastGtfsImportStatus = settings.LastGtfsImportStatus;
        LastGtfsImportError = settings.LastGtfsImportError;
        LastGtfsSourceVersion = settings.LastGtfsSourceVersion;
    }
}
