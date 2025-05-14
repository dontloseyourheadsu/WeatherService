using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WeatherService.Application.Models.MongoDb;

/// <summary>
/// Represents a weather forecast stored in MongoDB.
/// </summary>
public class MongoDbForecast
{
    /// <summary>
    /// Gets or sets the unique identifier for the forecast.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp of the forecast.
    /// </summary>
    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the latitude of the forecast location.
    /// </summary>
    [BsonElement("latitude")]
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude of the forecast location.
    /// </summary>
    [BsonElement("longitude")]
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the temperature at 2 meters above ground level.
    /// </summary>
    [BsonElement("temperature")]
    public double Temperature { get; set; }

    /// <summary>
    /// Gets or sets the temperature unit (e.g., Celsius, Fahrenheit).
    /// </summary>
    [BsonElement("temperatureUnit")]
    public string TemperatureUnit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the wind speed at 10 meters above ground level.
    /// </summary>
    [BsonElement("windSpeed")]
    public double WindSpeed { get; set; }

    /// <summary>
    /// Gets or sets the wind speed unit (e.g., km/h, m/s).
    /// </summary>
    [BsonElement("windSpeedUnit")]
    public string WindSpeedUnit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the wind direction at 10 meters above ground level.
    /// </summary>
    [BsonElement("windDirection")]
    public int WindDirection { get; set; }

    /// <summary>
    /// Gets or sets the wind direction unit (e.g., degrees, radians).
    /// </summary>
    [BsonElement("windDirectionUnit")]
    public string WindDirectionUnit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sunrise time for the forecast location.
    /// </summary>
    [BsonElement("sunrise")]
    public DateTime Sunrise { get; set; }
}
