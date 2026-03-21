namespace TransitAnalyticsAPI.Admin.Services;

public interface IGtfsUploadQueue
{
    bool TryEnqueue(GtfsUploadJob job);
}
