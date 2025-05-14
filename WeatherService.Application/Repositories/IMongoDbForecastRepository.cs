using WeatherService.Application.Models.MongoDb;
using WeatherService.Application.Utilities;

namespace WeatherService.Application.Repositories;

/// <summary>
/// Defines the contract for MongoDB operations on weather forecast documents.
/// </summary>
public interface IMongoDbForecastRepository
{
    /// <summary>
    /// Inserts a new forecast document into the collection.
    /// </summary>
    /// <param name="forecast">The forecast to insert.</param>
    Task<Result> InsertAsync(MongoDbForecast forecast);

    /// <summary>
    /// Retrieves a forecast by timestamp and location.
    /// </summary>
    /// <param name="timestamp">The forecast timestamp.</param>
    /// <param name="latitude">The latitude.</param>
    /// <param name="longitude">The longitude.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The forecast wrapped in a result, or failure result if not found or an error occurred.</returns>
    Task<Result<MongoDbForecast>> GetByTimeAndLocationAsync(DateTime timestamp, double latitude, double longitude, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing forecast in the collection.
    /// </summary>
    /// <param name="forecast">The forecast to update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation, with a result indicating success or failure.</returns>
    Task<Result> UpdateAsync(MongoDbForecast forecast, CancellationToken cancellationToken = default);
}
