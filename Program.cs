using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using TransitAnalyticsAPI.Admin.Security;
using TransitAnalyticsAPI.Admin.Services;
using TransitAnalyticsAPI.Background;
using TransitAnalyticsAPI.Clients.AucklandTransport;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Middleware;
using TransitAnalyticsAPI.Persistence;
using TransitAnalyticsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();
builder.Services.Configure<AdminOptions>(
    builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.Configure<AucklandTransportOptions>(
    builder.Configuration.GetSection(AucklandTransportOptions.SectionName));
builder.Services.AddSingleton(Channel.CreateBounded<GtfsUploadJob>(new BoundedChannelOptions(1)
{
    FullMode = BoundedChannelFullMode.DropWrite,
    SingleReader = true,
    SingleWriter = false
}));
builder.Services.AddSingleton<IPollingRuntimeState, PollingRuntimeState>();
builder.Services.AddSingleton<IGtfsUploadQueue, GtfsUploadQueue>();
builder.Services.AddScoped<IAdminSettingsService, AdminSettingsService>();
builder.Services.AddScoped<IGtfsUploadService, GtfsUploadService>();
builder.Services.AddSingleton<IAdminPasswordService, AdminPasswordService>();
builder.Services.AddScoped<IGtfsImportService, GtfsImportService>();
builder.Services.AddScoped<IVehicleLatestQueryService, VehicleLatestQueryService>();
builder.Services.AddScoped<IVehiclePositionMapper, VehiclePositionMapper>();
builder.Services.AddScoped<IVehiclePositionIngestionService, VehiclePositionIngestionService>();
builder.Services.AddScoped<IVehicleSnapshotBroadcastService, VehicleSnapshotBroadcastService>();
builder.Services.AddScoped<IVehicleWebSocketService, VehicleWebSocketService>();
builder.Services.AddSingleton<IWebSocketSubscriptionManager, WebSocketSubscriptionManager>();
builder.Services.AddHostedService<AdminSettingsBootstrapService>();
builder.Services.AddHostedService<GtfsUploadBackgroundService>();
builder.Services.AddHostedService<VehiclePollingService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        var adminOptions = builder.Configuration
            .GetSection(AdminOptions.SectionName)
            .Get<AdminOptions>() ?? new AdminOptions();

        options.Cookie.Name = adminOptions.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
    });
});
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
app.UseAuthentication();
app.UseMiddleware<MaintenanceModeMiddleware>();
app.UseAuthorization();
app.UseHttpsRedirection();

app.Map("/ws/vehicles", async context =>
{
    var adminSettingsService = context.RequestServices.GetRequiredService<IAdminSettingsService>();
    if (await adminSettingsService.IsMaintenanceModeEnabledAsync(context.RequestAborted))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "service_unavailable",
            message = "The service is in maintenance mode."
        });
        return;
    }

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

app.MapRazorPages();
app.MapControllers();

app.Run();
