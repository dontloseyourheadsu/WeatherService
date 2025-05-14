using System.Text.Json.Serialization;

namespace WeatherService.Application.Models.OpenMeteo;

/// <summary>
/// Represents daily weather data including sunrise times.
/// </summary>
public class OpenMeteoDailyData
{
    /// <summary>
    /// ISO date for each daily entry.
    /// </summary>
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    /// <summary>
    /// Sunrise time for each corresponding day.
    /// </summary>
    [JsonPropertyName("sunrise")]
    public List<string> Sunrise { get; set; } = new();
}