using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TransitAnalyticsAPI.Clients.AucklandTransport.Models;
using TransitAnalyticsAPI.Configuration;

namespace TransitAnalyticsAPI.Clients.AucklandTransport;

public class AucklandTransportClient : IAucklandTransportClient
{
    private const string VehicleLocationsPath = "/realtime/legacy/vehiclelocations";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false
    };

    private readonly HttpClient _httpClient;
    private readonly AucklandTransportOptions _options;

    public AucklandTransportClient(HttpClient httpClient, IOptions<AucklandTransportOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AucklandTransportApiResponse?> GetVehicleLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            VehicleLocationsPath);

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(_options.SubscriptionKey))
        {
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.SubscriptionKey);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<AucklandTransportApiResponse>(
            responseStream,
            JsonSerializerOptions,
            cancellationToken);
    }
}
