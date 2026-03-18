namespace TransitAnalyticsAPI.Models.Dto;

public class VehicleSnapshotMessageDto
{
    public string Type { get; set; } = "snapshot";

    public string Channel { get; set; } = string.Empty;

    public IReadOnlyList<VehicleLatestDto> Data { get; set; } = [];
}
