using Microsoft.Extensions.Logging;
using WeatherService.Application.Mapping;
using WeatherService.Application.Models;
using WeatherService.Application.Repositories;
using WeatherService.Application.Services.Geocoding;
using WeatherService.Application.Services.OpenMeteo;
using WeatherService.Application.Utilities;

namespace WeatherService.Application.Services;

/// <summary>
/// Default implementation of <see cref="IForecastService"/> that orchestrates
/// MongoDB caching and calls to the external Open‑Meteo API.
/// </summary>
public sealed class ForecastService : IForecastService
{
    /// <summary>
    /// MongoDB repository used for caching forecasts.
    /// </summary>
    private readonly IMongoDbForecastRepository _repository;

    /// <summary>
    /// Service for handling forecast time-related operations.
    /// </summary>
    private readonly IOpenMeteoForecastTimeService _forecastTimeService;

    /// <summary>
    /// Service for geocoding location names to coordinates.
    /// </summary>
    private readonly IGeocodingService _geocodingService;

    /// <summary>
    /// Service used to retrieve fresh forecasts from Open‑Meteo.
    /// </summary>
    private readonly IOpenMeteoWeatherForecastService _openMeteo;

    /// <summary>
    /// Logger for logging information and errors.
    /// </summary>
    private readonly ILogger<ForecastService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastService"/> class.
    /// </summary>
    /// <param name="repository">MongoDB repository used for caching forecasts.</param>
    /// <param name="forecastTimeService">Service for handling forecast time-related operations.</param>
    /// <param name="openMeteo">Service used to retrieve fresh forecasts from Open‑Meteo.</param>
    /// <param name="geocodingService">Service for geocoding location names to coordinates.</param>
    /// <param name="logger">Logger for logging information and errors.</param>
    public ForecastService(IMongoDbForecastRepository repository, IOpenMeteoForecastTimeService forecastTimeService, IGeocodingService geocodingService, IOpenMeteoWeatherForecastService openMeteo, ILogger<ForecastService> logger)
    {
        _logger = logger;
        _forecastTimeService = forecastTimeService;
        _repository = repository;
        _openMeteo = openMeteo;
        _geocodingService = geocodingService;
    }

    /// <inheritdoc />
    public async Task<Result<ForecastDetails>> GetWeatherForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var utcHour = GetCurrentUtcHour();

        var cachedResult = await TryGetCachedForecastAsync(utcHour, latitude, longitude, cancellationToken);

        if (cachedResult.IsSuccess)
        {
            return Result<ForecastDetails>.Success(cachedResult.Value);
        }

        var fetchResult = await FetchAndStoreForecastAsync(latitude, longitude, cancellationToken);
        return fetchResult;
    }

    /// <inheritdoc />
    public async Task<Result<ForecastDetails>> GetWeatherForecastAsync(string location, CancellationToken cancellationToken = default)
    {
        var geocodeResult = await _geocodingService.GetGeolocationAsync(location, cancellationToken);
        if (geocodeResult.IsFailure)
        {
            return Result<ForecastDetails>.Failure(geocodeResult.Errors);
        }
        var latitude = geocodeResult.Value.Latitude;
        var longitude = geocodeResult.Value.Longitude;
        return await GetWeatherForecastAsync(latitude, longitude, cancellationToken);
    }

    /// <summary>
    /// Gets the current UTC time truncated to the start of the hour (minutes and seconds set to zero).
    /// </summary>
    /// <returns>A <see cref="DateTime"/> representing the current UTC hour.</returns>
    private static DateTime GetCurrentUtcHour()
    {
        var nowUtc = DateTime.UtcNow;
        return new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Attempts to retrieve a cached forecast from the repository for the specified UTC hour and location.
    /// </summary>
    /// <param name="utcHour">The UTC hour timestamp to look up.</param>
    /// <param name="latitude">Latitude of the location.</param>
    /// <param name="longitude">Longitude of the location.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="Result{ForecastDetails}"/> indicating whether a cached forecast was found. 
    /// Returns null in the value if no forecast exists.
    /// </returns>
    private async Task<Result<ForecastDetails>> TryGetCachedForecastAsync(DateTime utcHour, double latitude, double longitude, CancellationToken cancellationToken)
    {
        var cached = await _repository.GetByTimeAndLocationAsync(utcHour, latitude, longitude, cancellationToken);
        if (cached.IsSuccess && cached.Value is not null)
        {
            _logger.LogDebug("Cache hit for forecast at {Latitude}, {Longitude} for {UtcHour}", latitude, longitude, utcHour);
            return Result<ForecastDetails>.Success(cached.Value.ToForecastDetails());
        }

        _logger.LogWarning("Error retrieving cached forecast: {Errors}", cached.Errors);

        // Return failure result if the cache lookup failed
        return Result<ForecastDetails>.Failure(cached.Errors);
    }

    /// <summary>
    /// Fetches weather forecast data from the external API, stores it in MongoDB, and maps it to the domain model.
    /// Normalizes the OpenMeteo forecast time to UTC+0 and retrieves the sunrise time for the forecast date.
    /// </summary>
    /// <param name="latitude">Latitude of the location.</param>
    /// <param name="longitude">Longitude of the location.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>A <see cref="Result{ForecastDetails}"/> containing the forecast or an error message.</returns>
    private async Task<Result<ForecastDetails>> FetchAndStoreForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var apiResult = await _openMeteo.GetWeatherForecastAsync(latitude, longitude, cancellationToken);
        if (apiResult.IsFailure)
        {
            _logger.LogWarning("Failed to fetch forecast from Open-Meteo: {Errors}", apiResult.Errors);
            return Result<ForecastDetails>.Failure(apiResult.Errors);
        }

        // Check if the API result is null
        var response = apiResult.Value;
        // Get the current local time for the forecast
        var localNow = _forecastTimeService.GetCurrentLocalTime(response);
        // Find the closest hourly index in the forecast data
        var hourlyIndex = _forecastTimeService.FindCurrentHourlyIndex(response, localNow);
        // Normalize the forecast time to UTC
        var normalizedUtc = _forecastTimeService.NormalizeToUtc(response, hourlyIndex);
        // Get the sunrise time for the forecast date
        var sunrise = _forecastTimeService.GetSunriseForDate(response, normalizedUtc);

        // Map the OpenMeteo forecast response to the MongoDB model
        var mongoModel = response.ToMongoForecast(hourlyIndex, normalizedUtc, sunrise, latitude, longitude);

        // Store the forecast in MongoDB
        var storeResult = await _repository.InsertAsync(mongoModel);
        if (storeResult.IsFailure)
        {
            _logger.LogWarning("Failed to store forecast in MongoDB: {Errors}", storeResult.Errors);
        }

        // Log the successful storage of the forecast
        return Result<ForecastDetails>.Success(mongoModel.ToForecastDetails());
    }
}