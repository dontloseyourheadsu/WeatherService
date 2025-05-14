using System.Text.Json.Serialization;

namespace WeatherService.Contracts.Responses.Errors;

/// <summary>
/// Represents a generic error response.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error messages.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}
