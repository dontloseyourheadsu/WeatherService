namespace WeatherService.Application.Models.Options;

/// <summary>
/// Represents the settings for connecting to the Open Meteo API.
/// </summary>
public class OpenMeteoSettings
{
    /// <summary>
    /// Gets or sets the base URL for the Open Meteo API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout for the Open Meteo API requests in seconds.
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 30;
}
