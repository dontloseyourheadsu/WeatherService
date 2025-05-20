using WeatherService.Application.Mapping;
using WeatherService.Application.Models;
using WeatherService.Application.Models.Geocoding;

namespace WeatherService.Tests.Application.Mappers;

public class GeocodingMapperTest
{
    [Fact]
    public void ToGeolocation_ShouldMapCorrectly()
    {
        // Arrange
        var geocodeResponse = new GeocodeForwardResponse
        {
            Latitude = 35.689487,
            Longitude = 139.691711,
            DisplayName = "Tokyo, Japan"
        };

        // Act
        var result = geocodeResponse.ToGeolocation();

        // Assert
        Assert.Equal(geocodeResponse.Latitude, result.Latitude);
        Assert.Equal(geocodeResponse.Longitude, result.Longitude);
        Assert.Equal(geocodeResponse.DisplayName, result.DisplayName);
    }

    [Fact]
    public void ToGeolocation_WithNullDisplayName_ShouldMapCorrectly()
    {
        // Arrange
        var geocodeResponse = new GeocodeForwardResponse
        {
            Latitude = 35.689487,
            Longitude = 139.691711,
            DisplayName = null
        };

        // Act
        var result = geocodeResponse.ToGeolocation();

        // Assert
        Assert.Equal(geocodeResponse.Latitude, result.Latitude);
        Assert.Equal(geocodeResponse.Longitude, result.Longitude);
        Assert.Null(result.DisplayName);
    }

    [Fact]
    public void ToGeolocation_WithEmptyDisplayName_ShouldMapCorrectly()
    {
        // Arrange
        var geocodeResponse = new GeocodeForwardResponse
        {
            Latitude = 35.689487,
            Longitude = 139.691711,
            DisplayName = string.Empty
        };

        // Act
        var result = geocodeResponse.ToGeolocation();

        // Assert
        Assert.Equal(geocodeResponse.Latitude, result.Latitude);
        Assert.Equal(geocodeResponse.Longitude, result.Longitude);
        Assert.Equal(string.Empty, result.DisplayName);
    }
}