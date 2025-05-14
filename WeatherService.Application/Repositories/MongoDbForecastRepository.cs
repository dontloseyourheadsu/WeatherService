using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WeatherService.Application.Data;
using WeatherService.Application.Models.MongoDb;
using WeatherService.Application.Models.Options;
using WeatherService.Application.Utilities;

namespace WeatherService.Application.Repositories;

/// <inheritdoc/>
public class MongoDbForecastRepository : IMongoDbForecastRepository
{
    /// <summary>
    /// The MongoDB collection used to store forecasts.
    /// </summary>
    private readonly IMongoCollection<MongoDbForecast> _collection;

    /// <summary>
    /// The logger used for logging information and errors.
    /// </summary>
    private readonly ILogger<MongoDbForecastRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbForecastRepository"/> class.
    /// </summary>
    /// <param name="databaseFactory">The database factory used to access the collection.</param>
    /// <param name="mongoDbSettings">The MongoDB settings used for configuration.</param>
    /// <param name="logger">The logger used for logging information and errors.</param>
    public MongoDbForecastRepository(IWeatherMongoDatabaseFactory databaseFactory, IOptions<MongoDbSettings> mongoDbSettingsOptions, ILogger<MongoDbForecastRepository> logger)
    {
        var database = databaseFactory.GetDatabase();
        var mongoDbSettings = mongoDbSettingsOptions.Value;
        _collection = database.GetCollection<MongoDbForecast>(mongoDbSettings.Collections.Forecasts);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> InsertAsync(MongoDbForecast forecast)
    {
        try
        {
            // Insert the forecast into the MongoDB collection
            await _collection.InsertOneAsync(forecast);

            // Log the successful insertion of the forecast
            _logger.LogDebug("Forecast inserted successfully with Id {Id}.", forecast.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // Log the error and return a failure result
            _logger.LogError(ex, "Error inserting forecast into MongoDB.");
            return Result.Failure($"Failed to insert forecast: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<MongoDbForecast>> GetByTimeAndLocationAsync(DateTime timestamp, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        try
        {
            // Build the filter to find the forecast by timestamp and location
            var filter = Builders<MongoDbForecast>.Filter.And(
                Builders<MongoDbForecast>.Filter.Eq(f => f.Timestamp, timestamp),
                Builders<MongoDbForecast>.Filter.Eq(f => f.Latitude, latitude),
                Builders<MongoDbForecast>.Filter.Eq(f => f.Longitude, longitude)
            );

            // Attempt to find the forecast in the collection
            var result = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            // Check if the result is null
            if (result is null)
            {
                _logger.LogWarning("Forecast not found for timestamp {Timestamp}, latitude {Latitude}, longitude {Longitude}.", timestamp, latitude, longitude);
                return Result<MongoDbForecast>.Failure("Forecast not found");
            }

            // Log the successful retrieval of the forecast
            _logger.LogDebug("Forecast retrieved successfully for timestamp {Timestamp}, latitude {Latitude}, longitude {Longitude}.", timestamp, latitude, longitude);
            return Result<MongoDbForecast>.Success(result);
        }
        catch (Exception ex)
        {
            // Log the error and return a failure result
            _logger.LogError(ex, "Error retrieving forecast for timestamp {Timestamp}, latitude {Latitude}, longitude {Longitude}.", timestamp, latitude, longitude);
            return Result<MongoDbForecast>.Failure($"Failed to retrieve forecast: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> UpdateAsync(MongoDbForecast forecast, CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure the forecast has a valid Id
            var forecastId = forecast.Id;
            var filter = Builders<MongoDbForecast>.Filter.Eq(f => f.Id, forecastId);

            // Check if the forecast exists
            await _collection.ReplaceOneAsync(filter, forecast, cancellationToken: cancellationToken);

            // Check if the forecast was found and updated
            if (forecast == null)
            {
                _logger.LogWarning("Forecast with Id {Id} not found for update.", forecastId);
                return Result.Failure("Forecast not found");
            }

            // Log the successful update of the forecast
            _logger.LogDebug("Forecast with Id {Id} updated successfully.", forecastId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            // Log the error and return a failure result
            _logger.LogError(ex, "Error updating forecast with Id {Id}.", forecast.Id);
            return Result.Failure($"Failed to update forecast: {ex.Message}");
        }
    }
}