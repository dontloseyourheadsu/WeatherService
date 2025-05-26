using System.Net;
using System.Text;
using System.Text.Json;
using NSubstitute;
using WeatherService.Application.Models;
using WeatherService.Application.Models.OpenMeteo;
using WeatherService.Application.Services.OpenMeteo;

namespace WeatherService.Tests.UnitTests.Services;

public class OpenMeteoWeatherForecastServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly OpenMeteoWeatherForecastService _sut;

    public OpenMeteoWeatherForecastServiceTests()
    {
        // Current date and time (for context in the test)
        var currentUtcDateTime = DateTime.Parse("2025-05-26 04:19:23");
        
        // Create a mock handler that we can configure
        _mockHttpMessageHandler = new MockHttpMessageHandler();
        
        // Create a real HTTP client with the mock handler
        _httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("https://api.open-meteo.com/v1/")
        };
        
        // Create the HTTP client factory mock
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient("OpenMeteo").Returns(_httpClient);
        
        // Create the system under test
        _sut = new OpenMeteoWeatherForecastService(_httpClientFactory);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithValidCoordinates_ReturnsSuccessfulResponse()
    {
        // Arrange
        var latitude = 52.52;
        var longitude = 13.41;
        var cancellationToken = CancellationToken.None;
        
        // Create a mock response with all properties as defined in the actual models
        var mockResponse = new OpenMeteoForecastResponse
        {
            Latitude = latitude,
            Longitude = longitude,
            UtcOffsetSeconds = 3600,
            Hourly = new OpenMeteoHourlyData
            {
                Time = new List<string> 
                { 
                    "2025-05-26T00:00:00Z", 
                    "2025-05-26T01:00:00Z",
                    "2025-05-26T02:00:00Z",
                    "2025-05-26T03:00:00Z",
                    "2025-05-26T04:00:00Z"
                },
                Temperature_2m = new List<double> { 20.5, 19.8, 19.2, 18.7, 18.5 },
                Wind_Speed_10m = new List<double> { 5.3, 4.8, 4.2, 3.9, 3.7 },
                Wind_Direction_10m = new List<int> { 120, 125, 130, 135, 140 }
            },
            HourlyUnits = new OpenMeteoHourlyUnits
            {
                Time = "iso8601",
                Temperature_2m = "°C",
                Wind_Speed_10m = "km/h",
                Wind_Direction_10m = "°"
            },
            Daily = new OpenMeteoDailyData
            {
                Time = new List<string> { "2025-05-26" },
                Sunrise = new List<string> { "2025-05-26T05:12:00Z" }
            }
        };
        
        // Configure the mock handler to return a successful response
        string responseContent = JsonSerializer.Serialize(mockResponse);
        _mockHttpMessageHandler.SetResponseContent(responseContent, HttpStatusCode.OK);

        // Act
        var result = await _sut.GetWeatherForecastAsync(latitude, longitude, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(latitude, result.Value.Latitude);
        Assert.Equal(longitude, result.Value.Longitude);
        Assert.Equal(3600, result.Value.UtcOffsetSeconds);
        Assert.Equal(5, result.Value.Hourly.Time.Count);
        Assert.Equal(5, result.Value.Hourly.Temperature_2m.Count);
        Assert.Equal(5, result.Value.Hourly.Wind_Speed_10m.Count);
        Assert.Equal(5, result.Value.Hourly.Wind_Direction_10m.Count);
        Assert.Equal("°C", result.Value.HourlyUnits.Temperature_2m);
        Assert.Equal(1, result.Value.Daily.Time.Count);
        Assert.Equal(1, result.Value.Daily.Sunrise.Count);
        Assert.Equal("2025-05-26T05:12:00Z", result.Value.Daily.Sunrise[0]);
        
        // Verify request was made with expected parameters
        var requestUri = _mockHttpMessageHandler.LastRequestUri;
        Assert.NotNull(requestUri);
        Assert.Contains($"latitude={latitude}", requestUri.ToString());
        Assert.Contains($"longitude={longitude}", requestUri.ToString());
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithApiError_ReturnsFailureResult()
    {
        // Arrange
        var latitude = 52.52;
        var longitude = 13.41;
        var cancellationToken = CancellationToken.None;
        
        // Create a mock error response
        var errorResponse = new OpenMeteoErrorResponse
        {
            Error = true,
            Reason = "Invalid location"
        };
        
        // Configure the mock handler to return an error response
        string responseContent = JsonSerializer.Serialize(errorResponse);
        _mockHttpMessageHandler.SetResponseContent(responseContent, HttpStatusCode.BadRequest);

        // Act
        var result = await _sut.GetWeatherForecastAsync(latitude, longitude, cancellationToken);

        // Assert
        var error = result.Errors.FirstOrDefault();
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid location", error);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithException_ReturnsFailureResult()
    {
        // Arrange
        var latitude = 52.52;
        var longitude = 13.41;
        var cancellationToken = CancellationToken.None;
        
        // Configure the mock handler to throw an exception
        _mockHttpMessageHandler.ShouldThrowException = true;
        _mockHttpMessageHandler.ExceptionToThrow = new HttpRequestException("Connection error");

        // Act
        var result = await _sut.GetWeatherForecastAsync(latitude, longitude, cancellationToken);

        // Assert
        var error = result.Errors.FirstOrDefault();
        Assert.False(result.IsSuccess);
        Assert.Contains("Exception occurred", error);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_ChecksForCorrectQueryParameters()
    {
        // Arrange
        var latitude = 52.52;
        var longitude = 13.41;
        var cancellationToken = CancellationToken.None;
        
        // Set up a basic response
        var mockResponse = new OpenMeteoForecastResponse
        {
            Latitude = latitude,
            Longitude = longitude,
            UtcOffsetSeconds = 3600
        };
        
        string responseContent = JsonSerializer.Serialize(mockResponse);
        _mockHttpMessageHandler.SetResponseContent(responseContent, HttpStatusCode.OK);

        // Act
        await _sut.GetWeatherForecastAsync(latitude, longitude, cancellationToken);

        // Assert
        var requestUri = _mockHttpMessageHandler.LastRequestUri;
        Assert.NotNull(requestUri);
        var requestUriString = requestUri.ToString();
        
        // Check that the request contains the expected parameters
        Assert.Contains($"latitude={latitude}", requestUriString);
        Assert.Contains($"longitude={longitude}", requestUriString);
        Assert.Contains("hourly=temperature_2m,wind_speed_10m,wind_direction_10m", requestUriString);
        Assert.Contains("daily=sunrise", requestUriString);
        Assert.Contains("timezone=auto", requestUriString);
    }

    /// <summary>
    /// Custom implementation of HttpMessageHandler for testing
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private string _responseContent = "{}";
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        public bool ShouldThrowException { get; set; } = false;
        public Exception ExceptionToThrow { get; set; } = new Exception("Test exception");
        public Uri? LastRequestUri { get; private set; }

        public void SetResponseContent(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = content;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            });
        }
    }
}