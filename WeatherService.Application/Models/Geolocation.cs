namespace WeatherService.Application.Models;

/// <summary>
/// Represents a geographical location with latitude, longitude, and display name.
/// </summary>
public class Geolocation
{
    /// <summary>
    /// Gets or sets the latitude of the location.
    /// </summary>
    public double Latitude { get; set; }
    /// <summary>
    /// Gets or sets the longitude of the location.
    /// </summary>
    public double Longitude { get; set; }
    /// <summary>
    /// Gets or sets the display name of the location.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
