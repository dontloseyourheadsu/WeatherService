using MongoDB.Driver;

namespace WeatherService.Application.Data;

/// <summary>
/// WeatherMongoDatabaseFactory is a factory class for creating MongoDB database connections related to weather database.
/// </summary>
public interface IWeatherMongoDatabaseFactory
{
    /// <summary>
    /// Gets the MongoDB database instance.
    /// </summary>
    /// <returns>MongoDB database instance.</returns>
    IMongoDatabase GetDatabase();
}
