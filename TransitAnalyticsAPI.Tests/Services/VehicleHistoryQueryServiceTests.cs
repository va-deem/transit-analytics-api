using TransitAnalyticsAPI.Models.Entities;
using TransitAnalyticsAPI.Services;
using TransitAnalyticsAPI.Tests.TestHelpers;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Services;

public class VehicleHistoryQueryServiceTests
{
    [Fact]
    public async Task GetVehicleHistoryAsync_ReturnsPointsOrderedByRecordedAtThenId()
    {
        await using var dbContext = TestDbContextFactory.CreateSqliteDbContext();
        var recordedAt = new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc);

        dbContext.VehiclePositions.AddRange(
            new VehiclePosition
            {
                Id = 30,
                VehicleId = "bus-1",
                Latitude = -36.1,
                Longitude = 174.1,
                RecordedAtUtc = recordedAt.AddMinutes(1),
                IngestedAtUtc = recordedAt.AddMinutes(1)
            },
            new VehiclePosition
            {
                Id = 20,
                VehicleId = "bus-1",
                Latitude = -36.2,
                Longitude = 174.2,
                RecordedAtUtc = recordedAt,
                IngestedAtUtc = recordedAt
            },
            new VehiclePosition
            {
                Id = 10,
                VehicleId = "bus-1",
                Latitude = -36.3,
                Longitude = 174.3,
                RecordedAtUtc = recordedAt,
                IngestedAtUtc = recordedAt.AddSeconds(10)
            });
        await dbContext.SaveChangesAsync();

        var service = new VehicleHistoryQueryService(dbContext, new FakeVehicleMetadataLookupService());

        var result = await service.GetVehicleHistoryAsync(
            "bus-1",
            recordedAt.AddMinutes(-5),
            recordedAt.AddMinutes(5),
            10,
            CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { -36.3, -36.2, -36.1 }, result.Select(item => item.Latitude));
    }

    [Fact]
    public async Task GetRangeAsync_FiltersByRouteId()
    {
        await using var dbContext = TestDbContextFactory.CreateSqliteDbContext();
        var recordedAt = new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc);

        dbContext.VehiclePositions.AddRange(
            new VehiclePosition
            {
                VehicleId = "bus-1",
                RouteId = "route-a",
                Latitude = -36.1,
                Longitude = 174.1,
                RecordedAtUtc = recordedAt,
                IngestedAtUtc = recordedAt
            },
            new VehiclePosition
            {
                VehicleId = "bus-2",
                RouteId = "route-b",
                Latitude = -36.2,
                Longitude = 174.2,
                RecordedAtUtc = recordedAt,
                IngestedAtUtc = recordedAt
            });
        await dbContext.SaveChangesAsync();

        var service = new VehicleHistoryQueryService(dbContext, new FakeVehicleMetadataLookupService());

        var result = await service.GetRangeAsync(
            recordedAt.AddMinutes(-5),
            recordedAt.AddMinutes(5),
            "route-a",
            10,
            CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal("bus-1", item.VehicleId);
        Assert.Equal("route-a", item.RouteId);
    }
}
