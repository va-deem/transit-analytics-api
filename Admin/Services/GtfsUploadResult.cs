using TransitAnalyticsAPI.Services;

namespace TransitAnalyticsAPI.Admin.Services;

public class GtfsUploadResult
{
    public required string SourceVersion { get; init; }

    public required GtfsImportResult ImportResult { get; init; }
}
