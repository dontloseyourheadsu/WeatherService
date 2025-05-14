namespace WeatherService.Application.Models.Options;

/// <summary>
/// Settings for MongoDB collections.
/// </summary>
public class MongoDbCollectionsSettings
{
    /// <summary>
    /// Gets or sets the name of the forecast collection.
    /// </summary>
    public string Forecasts { get; set; } = string.Empty;
}
