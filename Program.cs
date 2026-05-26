using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading.RateLimiting;
using System.Threading.Channels;
using TransitAnalyticsAPI.Admin.Security;
using TransitAnalyticsAPI.Admin.Services;
using TransitAnalyticsAPI.Background;
using TransitAnalyticsAPI.Clients.AucklandTransport;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Websockets;
using TransitAnalyticsAPI.Middleware;
using TransitAnalyticsAPI.Persistence;
using TransitAnalyticsAPI.Services;

var builder = WebApplication.CreateBuilder(args);
const long gtfsUploadMaxBytes = 100L * 1024 * 1024;

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = gtfsUploadMaxBytes;
});
builder.Services.Configure<AdminOptions>(
    builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.Configure<AucklandTransportOptions>(
    builder.Configuration.GetSection(AucklandTransportOptions.SectionName));
builder.Services.Configure<VehicleOptions>(
    builder.Configuration.GetSection(VehicleOptions.SectionName));
builder.Services.Configure<InternalApiOptions>(
    builder.Configuration.GetSection(InternalApiOptions.SectionName));
builder.Services.Configure<FeedbackOptions>(
    builder.Configuration.GetSection(FeedbackOptions.SectionName));
builder.Services.Configure<VehicleWebSocketOptions>(
    builder.Configuration.GetSection(VehicleWebSocketOptions.SectionName));
builder.Services.AddSingleton(Channel.CreateBounded<GtfsUploadJob>(new BoundedChannelOptions(1)
{
    FullMode = BoundedChannelFullMode.DropWrite,
    SingleReader = true,
    SingleWriter = false
}));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPollingRuntimeState, PollingRuntimeState>();
builder.Services.AddSingleton<IGtfsUploadQueue, GtfsUploadQueue>();
builder.Services.AddScoped<IAdminSettingsService, AdminSettingsService>();
builder.Services.AddScoped<IGtfsUploadService, GtfsUploadService>();
builder.Services.AddSingleton<IAdminPasswordService, AdminPasswordService>();
builder.Services.AddScoped<IGtfsImportService, GtfsImportService>();
builder.Services.AddScoped<IActiveImportRunResolver, ActiveImportRunResolver>();
builder.Services.AddScoped<IVehicleMetadataLookupService, VehicleMetadataLookupService>();
builder.Services.AddScoped<IRoutesQueryService, RoutesQueryService>();
builder.Services.AddScoped<IStopDeparturesQueryService, StopDeparturesQueryService>();
builder.Services.AddScoped<IVehicleHistoryQueryService, VehicleHistoryQueryService>();
builder.Services.AddScoped<IVehicleLatestQueryService, VehicleLatestQueryService>();
builder.Services.AddScoped<IVehicleRetentionService, VehicleRetentionService>();
builder.Services.AddScoped(typeof(ISystemLogService<>), typeof(SystemLogService<>));
builder.Services.AddScoped<IVehiclePositionMapper, VehiclePositionMapper>();
builder.Services.AddScoped<IVehiclePositionIngestionService, VehiclePositionIngestionService>();
builder.Services.AddScoped<IVehicleSnapshotBroadcastService, VehicleSnapshotBroadcastService>();
builder.Services.AddScoped<IVehicleWebSocketService, VehicleWebSocketService>();
builder.Services.AddSingleton<IWebSocketSubscriptionManager, WebSocketSubscriptionManager>();
builder.Services.AddHostedService<AdminSettingsBootstrapService>();
builder.Services.AddHostedService<GtfsUploadBackgroundService>();
builder.Services.AddHostedService<VehiclePollingService>();
builder.Services.AddHostedService<VehicleRetentionCleanupService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention());
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = gtfsUploadMaxBytes;
});
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
var feedbackOptions = builder.Configuration
    .GetSection(FeedbackOptions.SectionName)
    .Get<FeedbackOptions>() ?? new FeedbackOptions();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "rate_limited",
            message = "Too many submissions from this IP. Try again later."
        }, cancellationToken);
    };
    options.AddPolicy("feedback-submissions", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = feedbackOptions.RateLimitPermitLimit,
                Window = TimeSpan.FromMinutes(feedbackOptions.RateLimitWindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddHttpClient<IAucklandTransportClient, AucklandTransportClient>((serviceProvider, httpClient) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<AucklandTransportOptions>>()
        .Value;

    httpClient.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();
var internalApiOptions = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<InternalApiOptions>>()
    .Value;
var logger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("Startup");

if (app.Environment.IsProduction() && string.IsNullOrWhiteSpace(internalApiOptions.Secret))
{
    throw new InvalidOperationException(
        $"Configuration value '{InternalApiOptions.SectionName}:Secret' is required in production.");
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    logger.LogInformation("Applying database migrations.");
    await dbContext.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownProxies = { IPAddress.Loopback, IPAddress.IPv6Loopback }
});
app.UseRouting();
app.UseWebSockets();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<MaintenanceModeMiddleware>();
app.UseMiddleware<InternalApiSecretMiddleware>();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapVehicleWebSocket();

app.MapRazorPages();
app.MapControllers();

app.Run();
