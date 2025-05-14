using WeatherService.Application.Models.OpenMeteo;
using WeatherService.Application.Utilities;

namespace WeatherService.Application.Services.OpenMeteo;

/// <summary>
/// Defines methods for retrieving weather forecasts from the Open Meteo API.
/// </summary>
public interface IOpenMeteoWeatherForecastService
{
    /// <summary>
    /// Gets the weather forecast by longitude and latitude asynchronously.
    /// </summary>
    /// <param name="latitude">The latitude of the location.</param>
    /// <param name="longitude">The longitude of the location.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the weather forecast.</returns>
    Task<Result<OpenMeteoForecastResponse>> GetWeatherForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
