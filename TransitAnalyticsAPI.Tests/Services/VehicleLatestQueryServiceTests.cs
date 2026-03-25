using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Services;
using TransitAnalyticsAPI.Tests.TestHelpers;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Services;

public class VehicleLatestQueryServiceTests
{
    [Fact]
    public async Task GetLatestAsync_ReturnsNewestFreshRowPerVehicle()
    {
        await using var dbContext = TestDbContextFactory.CreateSqliteDbContext();
        var now = DateTime.UtcNow;

        dbContext.VehiclePositions.AddRange(
            new VehiclePosition
            {
                VehicleId = "bus-1",
                Latitude = -36.1,
                Longitude = 174.1,
                RecordedAtUtc = now.AddMinutes(-2),
                IngestedAtUtc = now.AddMinutes(-2)
            },
            new VehiclePosition
            {
                VehicleId = "bus-1",
                Latitude = -36.2,
                Longitude = 174.2,
                RecordedAtUtc = now.AddMinutes(-1),
                IngestedAtUtc = now.AddMinutes(-1)
            },
            new VehiclePosition
            {
                VehicleId = "bus-2",
                Latitude = -36.3,
                Longitude = 174.3,
                RecordedAtUtc = now.AddMinutes(-45),
                IngestedAtUtc = now.AddMinutes(-45)
            });
        await dbContext.SaveChangesAsync();

        var service = new VehicleLatestQueryService(
            dbContext,
            new FakeVehicleMetadataLookupService(),
            Options.Create(new VehicleOptions
            {
                LatestPositionMaxAgeMinutes = 5
            }));

        var result = await service.GetLatestAsync(CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("bus-1", item.VehicleId);
        Assert.Equal(-36.2, item.Latitude);
    }
}
