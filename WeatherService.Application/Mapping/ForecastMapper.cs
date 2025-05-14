using WeatherService.Application.Models;
using WeatherService.Application.Models.MongoDb;
using WeatherService.Application.Models.OpenMeteo;

namespace WeatherService.Application.Mapping
{
    /// <summary>
    /// Mapper class for forecast data.
    /// </summary>
    internal static class ForecastMapper
    {
        /// <summary>
        /// Creates a <see cref="MongoDbForecast"/> from the given Open-Meteo response and
        /// pre-calculated context supplied by <c>IForecastTimeService</c>.
        /// </summary>
        /// <param name="response">The raw Open-Meteo forecast.</param>
        /// <param name="hourlyIndex">Index of the hourly entry you wish to store.</param>
        /// <param name="timestampUtc">
        /// The hour-rounded UTC instant that represents the chosen hourly entry.
        /// </param>
        /// <param name="sunrise">Sunrise time for the same calendar day, in local time.</param>
        /// <param name="latitude">Latitude of the forecast location.</param>
        /// <param name="longitude">Longitude of the forecast location.</param>
        internal static MongoDbForecast ToMongoForecast(this OpenMeteoForecastResponse response, int hourlyIndex, DateTime timestampUtc, DateTime sunrise, double latitude, double longitude)
            => new()
            {
                Timestamp = timestampUtc,
                Latitude = latitude,
                Longitude = longitude,
                Temperature = response.Hourly.Temperature_2m[hourlyIndex],
                TemperatureUnit = response.HourlyUnits.Temperature_2m,
                WindSpeed = response.Hourly.Wind_Speed_10m[hourlyIndex],
                WindSpeedUnit = response.HourlyUnits.Wind_Speed_10m,
                WindDirection = response.Hourly.Wind_Direction_10m[hourlyIndex],
                WindDirectionUnit = response.HourlyUnits.Wind_Direction_10m,
                Sunrise = sunrise
            };

        /// <summary>
        /// Maps the MongoDbForecast model to a ForecastDetails model.
        /// </summary>
        /// <param name="mongo">MongoDbForecast instance.</param>
        /// <returns>Forecast details instance.</returns>
        internal static ForecastDetails ToForecastDetails(this MongoDbForecast mongo) =>
            new(
                Latitude: mongo.Latitude,
                Longitude: mongo.Longitude,
                Temperature: mongo.Temperature,
                TemperatureUnit: mongo.TemperatureUnit,
                WindDirection: mongo.WindDirection,
                WindDirectionUnit: mongo.WindDirectionUnit,
                WindSpeed: mongo.WindSpeed,
                WindSpeedUnit: mongo.WindSpeedUnit,
                Sunrise: mongo.Sunrise
            );
    }
}
