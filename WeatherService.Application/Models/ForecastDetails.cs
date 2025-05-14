namespace WeatherService.Application.Models;

/// <summary>
/// Represents the details of a weather forecast.
/// </summary>
/// <param name="Latitude">Geographical latitude of the forecast location.</param>
/// <param name="Longitude">Geographical longitude of the forecast location.</param>
/// <param name="Temperature">Temperature at the forecast location.</param> 
/// <param name="TemperatureUnit">Unit of the temperature (e.g., Celsius, Fahrenheit).</param>
/// <param name="WindDirection">Wind direction at the forecast location.</param>
/// <param name="WindDirectionUnit">Unit of the wind direction (e.g., degrees, radians).</param>
/// /// <param name="WindSpeed">Wind speed at the forecast location.</param>
/// /// <param name="WindSpeedUnit">Unit of the wind speed (e.g., km/h, m/s).</param>
/// /// <param name="Sunrise">Sunrise time at the forecast location.</param>
public record ForecastDetails(
    double Latitude,
    double Longitude,
    double Temperature,
    string TemperatureUnit,
    int WindDirection,
    string WindDirectionUnit,
    double WindSpeed,
    string WindSpeedUnit,
    DateTime Sunrise
);