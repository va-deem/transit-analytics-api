using TransitAnalyticsAPI.Clients.AucklandTransport.Models;
using TransitAnalyticsAPI.Services;
using Xunit;

namespace TransitAnalyticsAPI.Tests.Services;

public class VehiclePositionMapperTests
{
    [Fact]
    public void Map_SkipsDeletedAndIncompleteEntities_AndMapsValidOnes()
    {
        var mapper = new VehiclePositionMapper();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var result = mapper.Map(
            new[]
            {
                new AucklandTransportFeedEntity
                {
                    Id = "deleted",
                    IsDeleted = true,
                    Vehicle = new AucklandTransportVehicleWrapper
                    {
                        Vehicle = new AucklandTransportVehicleDescriptor { Id = "bus-0" },
                        Position = new AucklandTransportPosition { Latitude = -36.0, Longitude = 174.0 },
                        Timestamp = timestamp
                    }
                },
                new AucklandTransportFeedEntity
                {
                    Id = "missing-position",
                    Vehicle = new AucklandTransportVehicleWrapper
                    {
                        Vehicle = new AucklandTransportVehicleDescriptor { Id = "bus-1" },
                        Timestamp = timestamp
                    }
                },
                new AucklandTransportFeedEntity
                {
                    Id = "valid",
                    Vehicle = new AucklandTransportVehicleWrapper
                    {
                        Vehicle = new AucklandTransportVehicleDescriptor { Id = "bus-2" },
                        Trip = new AucklandTransportTripDescriptor
                        {
                            TripId = "trip-1",
                            RouteId = "route-1"
                        },
                        Position = new AucklandTransportPosition
                        {
                            Latitude = -36.85,
                            Longitude = 174.76,
                            Speed = 12.5
                        },
                        Timestamp = timestamp
                    }
                }
            });

        var item = Assert.Single(result);
        Assert.Equal("bus-2", item.VehicleId);
        Assert.Equal("trip-1", item.TripId);
        Assert.Equal("route-1", item.RouteId);
        Assert.Equal("valid", item.SourceEntityId);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime, item.RecordedAtUtc);
    }
}
