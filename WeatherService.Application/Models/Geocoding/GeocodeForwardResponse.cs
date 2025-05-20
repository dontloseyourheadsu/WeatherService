using System.Text.Json.Serialization;

namespace WeatherService.Application.Models.Geocoding;

/// <summary>
/// Represents the result of a geocoding forward request.
/// </summary>
public class GeocodeForwardResponse
{
    /// <summary>
    /// Gets or sets the display name of the location.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the latitude of the location
    /// </summary>
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude of the location.
    /// </summary>
    [JsonPropertyName("lon")]
    public double Longitude { get; set; }
}
