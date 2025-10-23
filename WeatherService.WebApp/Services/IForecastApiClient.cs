using WeatherService.Contracts.Responses;

namespace WeatherService.WebApp.Services;

public interface IForecastApiClient
{
    Task<string> EchoAsync(string message, CancellationToken cancellationToken = default);

    Task<ForecastDetailsResponse?> GetForecastByCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default);

    Task<ForecastDetailsResponse?> GetForecastByLocationAsync(string location, CancellationToken cancellationToken = default);
}
