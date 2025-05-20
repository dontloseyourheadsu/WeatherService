using WeatherService.Api.Mapping;
using WeatherService.Application.Models;
using WeatherService.Contracts.Responses;

namespace WeatherService.Tests.Api.Mappers;

public class ForecastMapperTest
{
    [Fact]
    public void ToForecastDetailsResponse_ShouldMapCorrectly()
    {
        // Arrange
        var forecastDetails = new ForecastDetails(
            Latitude: 35.689487,
            Longitude: 139.691711,
            Temperature: 16.2,
            TemperatureUnit: "°C",
            WindSpeed: 5.5,
            WindSpeedUnit: "km/h",
            WindDirection: 190,
            WindDirectionUnit: "°",
            Sunrise: new DateTime(2025, 5, 20, 5, 30, 0)
        );

        // Act
        var result = forecastDetails.ToForecastDetailsResponse();

        // Assert
        Assert.Equal(forecastDetails.Latitude, result.Latitude);
        Assert.Equal(forecastDetails.Longitude, result.Longitude);
        Assert.Equal(forecastDetails.Temperature, result.Temperature);
        Assert.Equal(forecastDetails.WindSpeed, result.WindSpeed);
        Assert.Equal(forecastDetails.WindDirection, result.WindDirection);
        Assert.Equal(forecastDetails.Sunrise, result.Sunrise);

        // Units
        Assert.NotNull(result.Units);
        Assert.Equal(forecastDetails.TemperatureUnit, result.Units.TemperatureUnit);
        Assert.Equal(forecastDetails.WindSpeedUnit, result.Units.WindSpeedUnit);
        Assert.Equal(forecastDetails.WindDirectionUnit, result.Units.WindDirectionUnit);
    }

    [Fact]
    public void ToForecastDetailsResponse_WithMinimumValues_ShouldMapCorrectly()
    {
        // Arrange
        var forecastDetails = new ForecastDetails(
            Latitude: -90.0,
            Longitude: -180.0,
            Temperature: -50.0,
            TemperatureUnit: "K",
            WindSpeed: 0.0,
            WindSpeedUnit: "m/s",
            WindDirection: 0,
            WindDirectionUnit: "rad",
            Sunrise: DateTime.MinValue
        );

        // Act
        var result = forecastDetails.ToForecastDetailsResponse();

        // Assert
        Assert.Equal(forecastDetails.Latitude, result.Latitude);
        Assert.Equal(forecastDetails.Longitude, result.Longitude);
        Assert.Equal(forecastDetails.Temperature, result.Temperature);
        Assert.Equal(forecastDetails.WindSpeed, result.WindSpeed);
        Assert.Equal(forecastDetails.WindDirection, result.WindDirection);
        Assert.Equal(forecastDetails.Sunrise, result.Sunrise);

        // Units
        Assert.NotNull(result.Units);
        Assert.Equal(forecastDetails.TemperatureUnit, result.Units.TemperatureUnit);
        Assert.Equal(forecastDetails.WindSpeedUnit, result.Units.WindSpeedUnit);
        Assert.Equal(forecastDetails.WindDirectionUnit, result.Units.WindDirectionUnit);
    }

    [Fact]
    public void ToForecastDetailsResponse_WithMaximumValues_ShouldMapCorrectly()
    {
        // Arrange
        var forecastDetails = new ForecastDetails(
            Latitude: 90.0,
            Longitude: 180.0,
            Temperature: 100.0,
            TemperatureUnit: "°F",
            WindSpeed: 200.0,
            WindSpeedUnit: "mph",
            WindDirection: 359,
            WindDirectionUnit: "°",
            Sunrise: DateTime.MaxValue
        );

        // Act
        var result = forecastDetails.ToForecastDetailsResponse();

        // Assert
        Assert.Equal(forecastDetails.Latitude, result.Latitude);
        Assert.Equal(forecastDetails.Longitude, result.Longitude);
        Assert.Equal(forecastDetails.Temperature, result.Temperature);
        Assert.Equal(forecastDetails.WindSpeed, result.WindSpeed);
        Assert.Equal(forecastDetails.WindDirection, result.WindDirection);
        Assert.Equal(forecastDetails.Sunrise, result.Sunrise);

        // Units
        Assert.NotNull(result.Units);
        Assert.Equal(forecastDetails.TemperatureUnit, result.Units.TemperatureUnit);
        Assert.Equal(forecastDetails.WindSpeedUnit, result.Units.WindSpeedUnit);
        Assert.Equal(forecastDetails.WindDirectionUnit, result.Units.WindDirectionUnit);
    }
}