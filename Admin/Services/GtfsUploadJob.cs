namespace TransitAnalyticsAPI.Admin.Services;

public class GtfsUploadJob
{
    public required string FileName { get; init; }

    public required string WorkRoot { get; init; }
}
