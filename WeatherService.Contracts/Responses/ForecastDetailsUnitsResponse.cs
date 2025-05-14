using System.Text.Json.Serialization;

namespace WeatherService.Contracts.Responses;

/// <summary>
/// Represents the response for forecast details units.
/// </summary>
public class ForecastDetailsUnitsResponse
{
    /// <summary>
    /// Gets or sets the temperature unit.
    /// </summary>
    [JsonPropertyName("temperatureUnit")]
    public string TemperatureUnit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the wind direction unit.
    /// </summary>
    [JsonPropertyName("windDirectionUnit")]
    public string WindDirectionUnit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the wind speed unit.
    /// </summary>
    [JsonPropertyName("windSpeedUnit")]
    public string WindSpeedUnit { get; set; } = string.Empty;
}
