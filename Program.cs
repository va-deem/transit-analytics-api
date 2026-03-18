using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Background;
using TransitAnalyticsAPI.Clients.AucklandTransport;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Persistence;
using TransitAnalyticsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<AucklandTransportOptions>(
    builder.Configuration.GetSection(AucklandTransportOptions.SectionName));
builder.Services.AddScoped<IGtfsImportService, GtfsImportService>();
builder.Services.AddScoped<IVehicleLatestQueryService, VehicleLatestQueryService>();
builder.Services.AddScoped<IVehiclePositionMapper, VehiclePositionMapper>();
builder.Services.AddScoped<IVehiclePositionIngestionService, VehiclePositionIngestionService>();
builder.Services.AddScoped<IVehicleSnapshotBroadcastService, VehicleSnapshotBroadcastService>();
builder.Services.AddScoped<IVehicleWebSocketService, VehicleWebSocketService>();
builder.Services.AddSingleton<IWebSocketSubscriptionManager, WebSocketSubscriptionManager>();
builder.Services.AddHostedService<VehiclePollingService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());
builder.Services.AddHttpClient<IAucklandTransportClient, AucklandTransportClient>((serviceProvider, httpClient) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<AucklandTransportOptions>>()
        .Value;

    httpClient.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseWebSockets();
app.UseHttpsRedirection();

app.Map("/ws/vehicles", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    using var scope = app.Services.CreateScope();
    var webSocketService = scope.ServiceProvider.GetRequiredService<IVehicleWebSocketService>();

    await webSocketService.HandleConnectionAsync(socket, context.RequestAborted);
});

app.MapControllers();

app.Run();
