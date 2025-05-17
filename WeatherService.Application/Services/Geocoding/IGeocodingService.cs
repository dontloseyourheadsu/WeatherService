using WeatherService.Application.Models;
using WeatherService.Application.Utilities;

namespace WeatherService.Application.Services.Geocoding;

public interface IGeocodingService
{
    /// <summary>
    /// Asynchronously retrieves the latitude and longitude of a given location.
    /// </summary>
    /// <param name="location">The location to geocode.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the latitude and longitude of the location.</returns>
    Task<Result<Geolocation>> GetGeolocationAsync(string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the display name of a location based on its latitude and longitude.
    /// </summary>
    /// <param name="latitude">Latitude of the location.</param>
    /// <param name="longitude">Longitude of the location.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the display name of the location.</returns>
    Task<Result<string>> GetLocationNameAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
