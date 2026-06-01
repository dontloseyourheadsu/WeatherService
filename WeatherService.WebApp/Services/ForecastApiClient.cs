using System.Net.Http.Json;
using WeatherService.Contracts.Responses;

namespace WeatherService.WebApp.Services;

public class ForecastApiClient(HttpClient httpClient) : IForecastApiClient
{
    private static class Routes
    {
        public const string Echo = "/api/forecast/echo";
        public const string ByCoordinates = "/api/forecast/coordinates";
        public const string ByLocation = "/api/forecast/location";
        public const string AvailableZones = "/api/forecast/zones";
        public const string ZoneAnalytics = "/api/forecast/zones/{0}/analytics";
    }

    public async Task<string> EchoAsync(string message, CancellationToken cancellationToken = default)
    {
        var url = $"{Routes.Echo}?message={Uri.EscapeDataString(message)}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<ForecastDetailsResponse?> GetForecastByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var url = $"{Routes.ByCoordinates}?latitude={latitude}&longitude={longitude}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ForecastDetailsResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ForecastDetailsResponse?> GetForecastByLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        var url = $"{Routes.ByLocation}?location={Uri.EscapeDataString(location)}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ForecastDetailsResponse>(cancellationToken: cancellationToken);
    }

    public async Task<List<string>?> GetAvailableZonesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(Routes.AvailableZones, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken);
    }

    public async Task<ZoneAnalyticsResponse?> GetZoneAnalyticsAsync(string zone, CancellationToken cancellationToken = default)
    {
        var url = string.Format(Routes.ZoneAnalytics, Uri.EscapeDataString(zone));
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<ZoneAnalyticsResponse>(cancellationToken: cancellationToken);
    }
}
