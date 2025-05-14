using WeatherService.Application.Models;
using WeatherService.Application.Utilities;

namespace WeatherService.Application.Services;

/// <summary>
/// Forecast service interface for retrieving weather forecasts.
/// </summary>
public interface IForecastService
{
    /// <summary>
    /// Gets the weather forecast by city name asynchronously.
    /// </summary>
    /// <param name="latitude">Latitude of the location.</param>
    /// <param name="longitude">Longitude of the location.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Task that represents the asynchronous operation. The task result contains the weather forecast.</returns>
    Task<Result<ForecastDetails>> GetWeatherForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
