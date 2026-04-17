using TransitAnalyticsAPI.Models.Dto;

namespace TransitAnalyticsAPI.Services;

public interface IStopDeparturesQueryService
{
    Task<List<StopDepartureDto>> GetUpcomingDeparturesAsync(
        string stopId,
        CancellationToken cancellationToken = default);
}
