using System.Text.Json.Serialization;

namespace WeatherService.Application.Models.Geocoding;

/// <summary>
/// Represents the response from a geocoding reverse request.
/// </summary>
internal class GeocodeReverseResponse
{
    /// <summary>
    /// Gets or sets the display name of the location.
    /// </summary>
    [JsonPropertyName("display_name")]
    internal string DisplayName { get; set; } = string.Empty;
}
