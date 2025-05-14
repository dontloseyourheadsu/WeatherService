using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WeatherService.Application.Models.Options;

namespace WeatherService.Application.Data;

/// <inheritdoc />
public class WeatherMongoDatabaseFactory : IWeatherMongoDatabaseFactory
{
    /// <summary>
    /// The MongoDB client used to connect to the database.
    /// </summary>
    private readonly IMongoClient _client;

    /// <summary>
    /// The name of the database to connect to.
    /// </summary>
    private readonly string _databaseName;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherMongoDatabaseFactory"/> class.
    /// Creates a MongoDB client and sets the database name.
    /// </summary>
    /// <param name="client">The MongoDB client used to connect to the database.</param>
    /// <param name="settings">The MongoDB settings containing the connection URI and database name.</param>
    public WeatherMongoDatabaseFactory(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        _client = client;
        _databaseName = settings.Value.DatabaseName;
    }

    /// <inheritdoc />
    public IMongoDatabase GetDatabase()
    {
        return _client.GetDatabase(_databaseName);
    }
}
