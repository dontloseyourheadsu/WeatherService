using System.Text.Json.Serialization;

namespace WeatherService.Application.Models.OpenMeteo;

/// <summary>
/// Represents an error response from the Open-Meteo API.
/// </summary>
public class OpenMeteoErrorResponse
{
    /// <summary>
    /// Indicates whether an error occurred.
    /// </summary>
    [JsonPropertyName("error")]
    public bool Error { get; set; }

    /// <summary>
    /// The error code returned by the Open-Meteo API.
    /// </summary>
    [JsonPropertyName("code")]
    public string Reason { get; set; } = string.Empty;
}
