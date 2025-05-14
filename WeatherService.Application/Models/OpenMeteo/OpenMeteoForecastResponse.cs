using System.Text.Json.Serialization;

namespace WeatherService.Application.Models.OpenMeteo;

/// <summary>
/// Represents the full weather forecast response from the Open-Meteo API.
/// </summary>
public class OpenMeteoForecastResponse
{
    /// <summary>
    /// Latitude of the requested location.
    /// </summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude of the requested location.
    /// </summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    /// <summary>
    /// Timezone of the requested location.
    /// </summary>
    [JsonPropertyName("utc_offset_seconds")]
    public int UtcOffsetSeconds { get; set; }

    /// <summary>
    /// Hourly forecast data block.
    /// </summary>
    [JsonPropertyName("hourly")]
    public OpenMeteoHourlyData Hourly { get; set; } = new();

    /// <summary>
    /// Daily forecast data block.
    /// </summary>
    [JsonPropertyName("daily")]
    public OpenMeteoDailyData Daily { get; set; } = new();

    /// <summary>
    /// Units for the hourly forecast data.
    /// </summary>
    [JsonPropertyName("hourly_units")]
    public OpenMeteoHourlyUnits HourlyUnits { get; set; } = new();
}
