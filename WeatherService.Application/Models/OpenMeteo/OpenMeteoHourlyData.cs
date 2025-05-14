using System.Text.Json.Serialization;

namespace WeatherService.Application.Models.OpenMeteo;

/// <summary>
/// Represents hourly weather forecast details.
/// </summary>
public class OpenMeteoHourlyData
{
    /// <summary>
    /// ISO timestamps for each hourly entry.
    /// </summary>
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    /// <summary>
    /// Temperature values at 2 meters for each hour.
    /// </summary>
    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature_2m { get; set; } = new();

    /// <summary>
    /// Wind speed values at 10 meters for each hour.
    /// </summary>
    [JsonPropertyName("wind_speed_10m")]
    public List<double> Wind_Speed_10m { get; set; } = new();

    /// <summary>
    /// Wind direction values at 10 meters for each hour.
    /// </summary>
    [JsonPropertyName("wind_direction_10m")]
    public List<int> Wind_Direction_10m { get; set; } = new();
}
