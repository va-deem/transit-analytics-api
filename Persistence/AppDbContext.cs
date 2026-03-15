using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Models.Entities;

namespace TransitAnalyticsAPI.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<VehiclePosition> VehiclePositions => Set<VehiclePosition>();
}
