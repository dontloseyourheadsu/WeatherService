namespace WeatherService.Application.Models.Options;

/// <summary>
/// Represents the settings for geocoding.
/// </summary>
public class GeocodingSettings
{
    /// <summary>
    /// Gets or sets the base URL for the geocoding service.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the endpoint for forward geocoding.
    /// </summary>
    public string ForwardEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the endpoint for reverse geocoding.
    /// </summary>
    public string ReverseEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the API key for the geocoding service.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
