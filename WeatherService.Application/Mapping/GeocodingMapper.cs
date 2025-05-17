using WeatherService.Application.Models;
using WeatherService.Application.Models.Geocoding;

namespace WeatherService.Application.Mapping;

/// <summary>
/// Maps geocoding-related data between different layers of the application.
/// </summary>
internal static class GeocodingMapper
{
    /// <summary>
    /// Maps a GeocodeForwardResult to a Geolocation object.
    /// </summary>
    /// <param name="result">Result of the geocoding forward request.</param>
    /// <returns>Geolocation object containing the latitude, longitude, and display name.</returns>
    internal static Geolocation ToGeolocation(this GeocodeForwardResponse result)
    {
        return new Geolocation
        {
            Latitude = result.Latitude,
            Longitude = result.Longitude,
            DisplayName = result.DisplayName
        };
    }
}
