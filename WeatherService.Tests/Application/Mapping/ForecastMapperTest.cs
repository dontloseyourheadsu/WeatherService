using WeatherService.Application.Mapping;
using WeatherService.Application.Models;
using WeatherService.Application.Models.MongoDb;
using WeatherService.Application.Models.OpenMeteo;

namespace WeatherService.Tests.Application.Mapping;

public class ForecastMapperTest
{
    [Fact]
    public void ToMongoForecast_ShouldMapCorrectly()
    {
        // Arrange
        var response = new OpenMeteoForecastResponse
        {
            Hourly = new OpenMeteoHourlyData
            {
                Temperature_2m = new List<double> { 15.5, 16.2, 17.0 },
                Wind_Speed_10m = new List<double> { 5.2, 5.5, 6.0 },
                Wind_Direction_10m = new List<int> { 180, 190, 200 }
            },
            HourlyUnits = new OpenMeteoHourlyUnits
            {
                Temperature_2m = "°C",
                Wind_Speed_10m = "km/h",
                Wind_Direction_10m = "°"
            }
        };

        int hourlyIndex = 1;
        DateTime timestampUtc = new DateTime(2025, 5, 20, 4, 0, 0, DateTimeKind.Utc);
        DateTime sunrise = new DateTime(2025, 5, 20, 5, 30, 0);
        double latitude = 35.689487;
        double longitude = 139.691711;

        // Act
        var result = response.ToMongoForecast(hourlyIndex, timestampUtc, sunrise, latitude, longitude);

        // Assert
        Assert.Equal(timestampUtc, result.Timestamp);
        Assert.Equal(latitude, result.Latitude);
        Assert.Equal(longitude, result.Longitude);
        Assert.Equal(response.Hourly.Temperature_2m[hourlyIndex], result.Temperature);
        Assert.Equal(response.HourlyUnits.Temperature_2m, result.TemperatureUnit);
        Assert.Equal(response.Hourly.Wind_Speed_10m[hourlyIndex], result.WindSpeed);
        Assert.Equal(response.HourlyUnits.Wind_Speed_10m, result.WindSpeedUnit);
        Assert.Equal(response.Hourly.Wind_Direction_10m[hourlyIndex], result.WindDirection);
        Assert.Equal(response.HourlyUnits.Wind_Direction_10m, result.WindDirectionUnit);
        Assert.Equal(sunrise, result.Sunrise);
    }

    [Fact]
    public void ToForecastDetails_ShouldMapCorrectly()
    {
        // Arrange
        var mongoForecast = new MongoDbForecast
        {
            Id = "646c67e83a0f8a0ae7ca45e9",
            Timestamp = new DateTime(2025, 5, 20, 4, 0, 0, DateTimeKind.Utc),
            Latitude = 35.689487,
            Longitude = 139.691711,
            Temperature = 16.2,
            TemperatureUnit = "°C",
            WindSpeed = 5.5,
            WindSpeedUnit = "km/h",
            WindDirection = 190,
            WindDirectionUnit = "°",
            Sunrise = new DateTime(2025, 5, 20, 5, 30, 0)
        };

        // Act
        var result = mongoForecast.ToForecastDetails();

        // Assert
        Assert.Equal(mongoForecast.Latitude, result.Latitude);
        Assert.Equal(mongoForecast.Longitude, result.Longitude);
        Assert.Equal(mongoForecast.Temperature, result.Temperature);
        Assert.Equal(mongoForecast.TemperatureUnit, result.TemperatureUnit);
        Assert.Equal(mongoForecast.WindSpeed, result.WindSpeed);
        Assert.Equal(mongoForecast.WindSpeedUnit, result.WindSpeedUnit);
        Assert.Equal(mongoForecast.WindDirection, result.WindDirection);
        Assert.Equal(mongoForecast.WindDirectionUnit, result.WindDirectionUnit);
        Assert.Equal(mongoForecast.Sunrise, result.Sunrise);
    }
}