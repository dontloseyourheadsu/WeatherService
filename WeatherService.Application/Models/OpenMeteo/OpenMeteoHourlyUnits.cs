using System.Text.Json.Serialization;

namespace WeatherService.Application.Models;

/// <summary>
/// Represents the units for hourly weather data.
/// </summary>
public class OpenMeteoHourlyUnits
{
    /// <summary>
    /// Units for the time field.
    /// </summary>
    [JsonPropertyName("time")]
    public string Time { get; set; } = null!;

    /// <summary>
    /// Units for the wind direction at 10 meters.
    /// </summary>
    [JsonPropertyName("wind_direction_10m")]
    public string Wind_Direction_10m { get; set; } = null!;

    /// <summary>
    /// Units for the wind speed at 10 meters.
    /// </summary>
    [JsonPropertyName("wind_speed_10m")]
    public string Wind_Speed_10m { get; set; } = null!;

    /// <summary>
    /// Units for the temperature at 2 meters.
    /// </summary>
    [JsonPropertyName("temperature_2m")]
    public string Temperature_2m { get; set; } = null!;
}