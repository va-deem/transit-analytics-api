using Microsoft.EntityFrameworkCore;
using TransitAnalyticsAPI.Clients.AucklandTransport;
using TransitAnalyticsAPI.Configuration;
using TransitAnalyticsAPI.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<AucklandTransportOptions>(
    builder.Configuration.GetSection(AucklandTransportOptions.SectionName));
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
