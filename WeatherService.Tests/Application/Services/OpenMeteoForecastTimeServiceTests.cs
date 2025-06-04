using WeatherService.Application.Models.OpenMeteo;
using WeatherService.Application.Services.OpenMeteo;

namespace WeatherService.Tests.UnitTests.Services;

public class OpenMeteoForecastTimeServiceTests
{
    private readonly OpenMeteoForecastTimeService _sut;
    private readonly OpenMeteoForecastResponse _mockResponse;

    public OpenMeteoForecastTimeServiceTests()
    {
        // Create the system under test
        _sut = new OpenMeteoForecastTimeService();

        // Create a standard mock response to be used in tests
        _mockResponse = new OpenMeteoForecastResponse
        {
            Latitude = 52.52,
            Longitude = 13.41,
            UtcOffsetSeconds = 3600, // UTC+1
            Hourly = new OpenMeteoHourlyData
            {
                Time = new List<string>
                {
                    "2025-06-04T18:00", // This format is important - no Z or timezone
                    "2025-06-04T19:00",
                    "2025-06-04T20:00",
                    "2025-06-04T21:00",
                    "2025-06-04T22:00"
                },
                Temperature_2m = new List<double> { 20.5, 19.8, 19.2, 18.7, 18.5 },
                Wind_Speed_10m = new List<double> { 5.3, 4.8, 4.2, 3.9, 3.7 },
                Wind_Direction_10m = new List<int> { 120, 125, 130, 135, 140 }
            },
            Daily = new OpenMeteoDailyData
            {
                Time = new List<string> { "2025-06-04", "2025-06-05" },
                Sunrise = new List<string> { "2025-06-04T04:12", "2025-06-05T04:11" }
            }
        };
    }

    [Fact]
    public void GetCurrentLocalTime_ReturnsCorrectLocalTime()
    {
        // Arrange - Fixed timestamp for testing
        DateTime utcNow = DateTime.UtcNow;

        // Act
        var actualLocalTime = _sut.GetCurrentLocalTime(_mockResponse);

        // Assert
        // Check that the offset is applied correctly (within a small tolerance)
        var expectedOffset = TimeSpan.FromSeconds(_mockResponse.UtcOffsetSeconds);
        var actualOffset = actualLocalTime - utcNow;

        // Assert within a reasonable tolerance (e.g., 2 seconds)
        Assert.True(Math.Abs((actualOffset - expectedOffset).TotalSeconds) < 2,
            $"Expected offset close to {expectedOffset.TotalSeconds}s but was {actualOffset.TotalSeconds}s");
    }

    [Fact]
    public void FindCurrentHourlyIndex_WhenExactHourExists_ReturnsCorrectIndex()
    {
        // Arrange
        // The key insight: FindCurrentHourlyIndex compares just the date and hour
        // Create a local time that matches the hour of the second entry (index 1)
        var localTime = new DateTime(2025, 6, 4, 19, 0, 0, DateTimeKind.Unspecified); // 19:00

        // Act
        int result = _sut.FindCurrentHourlyIndex(_mockResponse, localTime);

        // Assert
        Assert.Equal(1, result); // Should be index 1 which is 19:00
    }

    [Fact]
    public void FindCurrentHourlyIndex_WhenPartialHourExists_ReturnsCorrectIndex()
    {
        // Arrange
        // Even with minutes and seconds, it should still match the hour
        var localTime = new DateTime(2025, 6, 4, 19, 30, 45, DateTimeKind.Unspecified); // 19:30:45

        // Act
        int result = _sut.FindCurrentHourlyIndex(_mockResponse, localTime);

        // Assert
        Assert.Equal(1, result); // Should still be index 1 (19:00)
    }

    [Fact]
    public void FindCurrentHourlyIndex_WhenHourNotFound_ReturnsMinusOne()
    {
        // Arrange
        // Use a time that doesn't match any hour in our sample data
        var localTime = new DateTime(2025, 6, 5, 2, 0, 0, DateTimeKind.Unspecified);

        // Act
        int result = _sut.FindCurrentHourlyIndex(_mockResponse, localTime);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void NormalizeToUtc_ConvertsLocalTimeToUtc()
    {
        // Arrange
        int hourlyIndex = 1; // This corresponds to "2025-06-04T19:00" in the mock data

        // Act
        var normalizedResult = _sut.NormalizeToUtc(_mockResponse, hourlyIndex);

        // Assert
        Assert.Equal(DateTimeKind.Utc, normalizedResult.Kind);

        // The original time is 19:00 local time, which is 18:00 UTC (offset is -1 hour)
        // The service subtracts the UTC offset from local time to get UTC time
        Assert.Equal(2025, normalizedResult.Year);
        Assert.Equal(6, normalizedResult.Month);
        Assert.Equal(4, normalizedResult.Day);
        Assert.Equal(18, normalizedResult.Hour); // 19:00 local - 1 hour offset = 18:00 UTC
        Assert.Equal(0, normalizedResult.Minute);
        Assert.Equal(0, normalizedResult.Second);
    }

    [Fact]
    public void NormalizeToUtc_ClearsMinutesAndSeconds()
    {
        // Arrange
        // Simulate a case where the API response might have minutes/seconds
        var customResponse = new OpenMeteoForecastResponse
        {
            UtcOffsetSeconds = 3600,
            Hourly = new OpenMeteoHourlyData
            {
                Time = new List<string> { "2025-06-04T19:30:45" } // Local time with minutes/seconds
            }
        };

        // Act
        var normalizedResult = _sut.NormalizeToUtc(customResponse, 0);

        // Assert
        Assert.Equal(0, normalizedResult.Minute);
        Assert.Equal(0, normalizedResult.Second);
    }

    [Fact]
    public void GetSunriseForDate_WithMatchingDate_ReturnsSunriseTime()
    {
        // Arrange
        var utcDate = new DateTime(2025, 6, 4, 12, 0, 0, DateTimeKind.Utc); // A time on June 4 UTC

        // Act
        var sunrise = _sut.GetSunriseForDate(_mockResponse, utcDate);

        // Assert
        var expectedSunrise = DateTime.Parse("2025-06-04T04:12");
        Assert.Equal(expectedSunrise, sunrise);
    }

    [Fact]
    public void GetSunriseForDate_WithNextDay_ReturnsNextDaySunrise()
    {
        // Arrange
        var utcDate = new DateTime(2025, 6, 5, 2, 0, 0, DateTimeKind.Utc); // A time on June 5 UTC

        // Act
        var sunrise = _sut.GetSunriseForDate(_mockResponse, utcDate);

        // Assert
        var expectedSunrise = DateTime.Parse("2025-06-05T04:11");
        Assert.Equal(expectedSunrise, sunrise);
    }

    [Fact]
    public void GetSunriseForDate_WithDateNotInResponse_ReturnsFallbackDate()
    {
        // Arrange
        var utcDate = new DateTime(2025, 6, 6, 12, 0, 0, DateTimeKind.Utc); // A date not in our sample data
        var localDate = (utcDate + TimeSpan.FromSeconds(_mockResponse.UtcOffsetSeconds)).Date;

        // Act
        var sunrise = _sut.GetSunriseForDate(_mockResponse, utcDate);

        // Assert
        // According to the implementation, if no sunrise is found, it should return the local date
        Assert.Equal(localDate, sunrise.Date);
    }
}