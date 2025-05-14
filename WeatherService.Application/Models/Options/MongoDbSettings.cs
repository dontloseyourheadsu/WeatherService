namespace WeatherService.Application.Models.Options;

/// <summary>
/// Represents the settings for connecting to a MongoDB database.
/// </summary>
public class MongoDbSettings
{
    /// <summary>
    /// Gets or sets the connection URI for the MongoDB database.
    /// </summary>
    public string ConnectionUri { get; init; } = default!;

    /// <summary>
    /// Gets or sets the name of the database to connect to.
    /// </summary>
    public string DatabaseName { get; init; } = default!;

    /// <summary>
    /// Gets or sets the settings for the collections within the database.
    /// </summary>
    public MongoDbCollectionsSettings Collections { get; init; } = default!;
}
