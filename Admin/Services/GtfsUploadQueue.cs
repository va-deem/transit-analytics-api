using System.Threading.Channels;

namespace TransitAnalyticsAPI.Admin.Services;

public class GtfsUploadQueue : IGtfsUploadQueue
{
    private readonly Channel<GtfsUploadJob> _channel;

    public GtfsUploadQueue(Channel<GtfsUploadJob> channel)
    {
        _channel = channel;
    }

    public bool TryEnqueue(GtfsUploadJob job)
    {
        return _channel.Writer.TryWrite(job);
    }
}
