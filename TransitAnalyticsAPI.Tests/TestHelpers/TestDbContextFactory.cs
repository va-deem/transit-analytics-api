using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Persistence;

namespace TransitAnalyticsAPI.Tests.TestHelpers;

internal static class TestDbContextFactory
{
    public static AppDbContext CreateSqliteDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}
