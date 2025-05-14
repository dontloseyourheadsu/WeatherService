using System.Text.Json;
using System.Web;
using WeatherService.Application.Models.OpenMeteo;
using WeatherService.Application.Utilities;

namespace WeatherService.Application.Services.OpenMeteo;

/// <summary>
/// Service for retrieving weather forecasts from the Open-Meteo API.
/// </summary>
public class OpenMeteoWeatherForecastService : IOpenMeteoWeatherForecastService
{
    /// <summary>
    /// The HTTP client used to make requests to the Open-Meteo API.
    /// </summary>
    private readonly HttpClient _openMeteoHttpClient;

    /// <summary>
    /// The endpoint for the weather forecast API.
    /// </summary>
    private const string ForecastEndpoint = "forecast";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenMeteoWeatherForecastService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients.</param>
    public OpenMeteoWeatherForecastService(IHttpClientFactory httpClientFactory)
    {
        _openMeteoHttpClient = httpClientFactory.CreateClient("OpenMeteo");
    }

    /// <inheritdoc />
    public async Task<Result<OpenMeteoForecastResponse>> GetWeatherForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            //Build the request URI with query parameters
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["latitude"] = latitude.ToString("F6");
            query["longitude"] = longitude.ToString("F6");
            query["hourly"] = "temperature_2m,wind_speed_10m,wind_direction_10m";
            query["daily"] = "sunrise";
            query["timezone"] = "auto";

            var requestUri = $"{ForecastEndpoint}?{query}";

            // Send the request to the Open-Meteo API
            var response = await _openMeteoHttpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // If the response is not successful, read the error response
                var errorStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var errorResponse = await JsonSerializer.DeserializeAsync<OpenMeteoErrorResponse>(errorStream, cancellationToken: cancellationToken);
                var errorMessage = errorResponse?.Reason ?? "Unknown OpenMeteo error";

                // Log the error message
                return Result<OpenMeteoForecastResponse>.Failure($"OpenMeteoAPI error: {errorMessage}");
            }

            // Read the response content as a stream and deserialize it
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var forecast = await JsonSerializer.DeserializeAsync<OpenMeteoForecastResponse>(stream, cancellationToken: cancellationToken);

            // Check if the forecast is null and return a failure result if it is
            return forecast != null
                ? Result<OpenMeteoForecastResponse>.Success(forecast)
                : Result<OpenMeteoForecastResponse>.Failure("Failed to deserialize OpenMeteoAPI response");
        }
        catch (Exception ex)
        {
            return Result<OpenMeteoForecastResponse>.Failure($"Exception occurred: {ex.Message}");
        }
    }
}