using System.Text.Json.Serialization;

namespace WeatherService.Contracts.Responses;

/// <summary>
/// Represents the response for forecast details.
/// </summary>
public class ForecastDetailsResponse
{
    /// <summary>
    /// Gets or sets the geographical latitude of the forecast location.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the geographical longitude of the forecast location.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the temperature at the forecast location.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    /// <summary>
    /// Gets or sets the wind direction at the forecast location.
    /// </summary>
    [JsonPropertyName("windDirection")]
    public int WindDirection { get; set; }

    /// <summary>
    /// Gets or sets the wind speed at the forecast location.
    /// </summary>
    [JsonPropertyName("windSpeed")]
    public double WindSpeed { get; set; }

    /// <summary>
    /// Gets or sets the sunrise time at the forecast location.
    /// </summary>
    [JsonPropertyName("sunrise")]
    public DateTime Sunrise { get; set; }

    /// <summary>
    /// Gets or sets the forecast details units.
    /// </summary>
    [JsonPropertyName("units")]
    public ForecastDetailsUnitsResponse Units { get; set; } = new();
}
