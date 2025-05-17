using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WeatherService.Application.Mapping;
using WeatherService.Application.Models;
using WeatherService.Application.Models.Geocoding;
using WeatherService.Application.Models.Options;
using WeatherService.Application.Utilities;

namespace WeatherService.Application.Services.Geocoding;

/// <inheritdoc />
internal class GeocodingService : IGeocodingService
{
    /// <summary>
    /// The base URL for the geocoding service.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// The settings for the geocoding service.
    /// </summary>
    private readonly GeocodingSettings _geocodingSettings;

    /// <summary>
    /// The logger instance for logging information and errors.
    /// </summary>
    private readonly ILogger<GeocodingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeocodingService"/> class.
    /// </summary>
    /// <param name="geocodingOptions">Geocoding settings options.</param>
    /// <param name="httpClientFactory">HTTP client factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance for logging information and errors.</param>
    public GeocodingService(IOptions<GeocodingSettings> geocodingOptions, IHttpClientFactory httpClientFactory, ILogger<GeocodingService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Geocoding");
        _logger = logger;
        _geocodingSettings = geocodingOptions.Value;
    }

    /// <inheritdoc />
    public async Task<Result<Geolocation>> GetGeolocationAsync(string location, CancellationToken cancellationToken = default)
    {
        try
        {
            // Construct the request URL
            var url = $"{_geocodingSettings.ForwardEndpoint}?q={location}&api_key={_geocodingSettings.ApiKey}";

            // Read the response from the geocoding service as a stream int a GeocodeForwardResponse object
            using var responseStream = await _httpClient.GetStreamAsync(url, cancellationToken);

            // Deserialize the response stream into a GeocodeForwardResponse object
            var geocodeResponses = await JsonSerializer.DeserializeAsync<List<GeocodeForwardResponse>>(responseStream, new JsonSerializerOptions
            {
                DefaultBufferSize = 81920,
            }, cancellationToken);

            // Check if the response contains results
            if (geocodeResponses == null || geocodeResponses.Count == 0)
            {
                _logger.LogWarning("No results found for the specified location: {Location}", location);
                return Result<Geolocation>.Failure("No results found for the specified location.");
            }

            // Extract the first result
            var geocode = geocodeResponses[0];

            // Map the geocode result to a Geolocation object
            var geolocation = geocode.ToGeolocation();

            // Log the successful geocoding operation
            _logger.LogInformation("Successfully retrieved geolocation for {Location}: {Latitude}, {Longitude}", location, geolocation.Latitude, geolocation.Longitude);
            return Result<Geolocation>.Success(geolocation);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error occurred while calling the geocoding service.");
            return Result<Geolocation>.Failure("An error occurred while retrieving geolocation data.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error occurred while deserializing the geocoding response.");
            return Result<Geolocation>.Failure("An error occurred while processing geolocation data.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            return Result<Geolocation>.Failure("An unexpected error occurred while getting the geolocation data.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetLocationNameAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            // Construct the request URL
            var url = $"{_geocodingSettings.ReverseEndpoint}?lat={latitude}&lon={longitude}&api_key={_geocodingSettings.ApiKey}";
            // Read the response from the geocoding service as a stream into a GeocodeReverseResponse object
            using var responseStream = await _httpClient.GetStreamAsync(url, cancellationToken);
            // Deserialize the response stream into a GeocodeReverseResponse object
            var geocodeResponse = await JsonSerializer.DeserializeAsync<GeocodeReverseResponse>(responseStream, new JsonSerializerOptions
            {
                DefaultBufferSize = 81920,
            }, cancellationToken);

            // Check if the response contains results
            if (string.IsNullOrEmpty(geocodeResponse?.DisplayName))
            {
                _logger.LogWarning("No results found for the specified coordinates: {Latitude}, {Longitude}", latitude, longitude);
                return Result<string>.Failure("No results found for the specified coordinates.");
            }

            // Extract the display name from the response
            var locationName = geocodeResponse.DisplayName;

            // Log the successful geocoding operation
            _logger.LogInformation("Successfully retrieved location name for coordinates {Latitude}, {Longitude}: {LocationName}", latitude, longitude, locationName);
            return Result<string>.Success(locationName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error occurred while calling the geocoding service.");
            return Result<string>.Failure("An error occurred while retrieving location name data.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error occurred while deserializing the geocoding response.");
            return Result<string>.Failure("An error occurred while processing location name data.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            return Result<string>.Failure("An unexpected error occurred while getting the location name data.");
        }
    }
}