namespace WeatherService.Application.Models.Geocoding;

/// <summary>
/// Represents the response from a geocoding forward request.
/// </summary>
internal class GeocodeForwardResponse
{
    /// <summary>
    /// Gets or sets the status of the geocoding request.
    /// </summary>
    public List<GeocodeForwardResult> Results { get; set; } = new List<GeocodeForwardResult>();
}
