using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using WeatherService.Application.Models.Geocoding;
using WeatherService.Application.Models.Options;
using WeatherService.Application.Services.Geocoding;

namespace WeatherService.Tests.UnitTests.Services;

public class GeocodingServiceTests
{
    private readonly IOptions<GeocodingSettings> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeocodingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly HttpMessageHandlerStub _httpMessageHandler;
    private readonly IGeocodingService _sut;

    public GeocodingServiceTests()
    {
        // Set up substitutes
        _options = Substitute.For<IOptions<GeocodingSettings>>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = Substitute.For<ILogger<GeocodingService>>();
        _httpMessageHandler = new HttpMessageHandlerStub();
        _httpClient = new HttpClient(_httpMessageHandler);

        // Configure settings
        _options.Value.Returns(new GeocodingSettings
        {
            ApiKey = "test-api-key",
            ForwardEndpoint = "https://api.geocoding.com/forward",
            ReverseEndpoint = "https://api.geocoding.com/reverse"
        });

        // Configure HttpClient factory to return our HTTP client
        _httpClientFactory.CreateClient("Geocoding").Returns(_httpClient);

        // Create system under test
        _sut = new GeocodingService(
            _options,
            _httpClientFactory,
            _logger
        );
    }

    [Fact]
    public async Task GetGeolocationAsync_WhenLocationExists_ReturnsSuccessResult()
    {
        // Arrange
        var location = "New York";
        var geocodeResponses = new List<GeocodeForwardResponse>
        {
            new GeocodeForwardResponse
            {
                DisplayName = "New York City, NY, USA",
                Latitude = 40.7128,
                Longitude = -74.0060
            }
        };

        // Set up HTTP response
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(geocodeResponses), Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _sut.GetGeolocationAsync(location);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(geocodeResponses[0].DisplayName, result.Value.DisplayName);
        Assert.Equal(geocodeResponses[0].Latitude, result.Value.Latitude);
        Assert.Equal(geocodeResponses[0].Longitude, result.Value.Longitude);
    }

    [Fact]
    public async Task GetGeolocationAsync_WhenNoResultsFound_ReturnsFailureResult()
    {
        // Arrange
        var location = "NonexistentPlace";
        var emptyResponse = new List<GeocodeForwardResponse>();

        // Set up HTTP response
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(emptyResponse), Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _sut.GetGeolocationAsync(location);

        // Assert
        var error = result.Errors.FirstOrDefault();
        Assert.False(result.IsSuccess);
        Assert.Equal("No results found for the specified location.", error);
    }

    [Fact]
    public async Task GetGeolocationAsync_WhenHttpRequestFails_ReturnsFailureResult()
    {
        // Arrange
        var location = "New York";

        // Set up HTTP response
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = new StringContent("Internal Server Error")
        };
        _httpMessageHandler.ShouldThrowException = true;

        // Act
        var result = await _sut.GetGeolocationAsync(location);

        // Assert
        var error = result.Errors.FirstOrDefault();
        Assert.False(result.IsSuccess);
        Assert.Equal("An error occurred while retrieving geolocation data.", error);
    }

    [Fact]
    public async Task GetGeolocationAsync_WhenJsonDeserializationFails_ReturnsFailureResult()
    {
        // Arrange
        var location = "New York";

        // Set up HTTP response with invalid JSON
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("Invalid JSON", Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _sut.GetGeolocationAsync(location);

        // Assert
        var error = result.Errors.FirstOrDefault();
        Assert.False(result.IsSuccess);
        Assert.Equal("An error occurred while processing geolocation data.", error);
    }

    [Fact]
    public async Task GetLocationNameAsync_WhenDisplayNameIsEmpty_ReturnsFailureResult()
    {
        // Arrange
        double latitude = 0;
        double longitude = 0;

        // Create a GeocodeReverseResponse object with empty DisplayName
        var response = new GeocodeReverseResponse
        {
            DisplayName = ""
        };

        // Set up HTTP response
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _sut.GetLocationNameAsync(latitude, longitude);

        // Assert
        var error = result.Errors.FirstOrDefault();
        Assert.False(result.IsSuccess);
        Assert.Equal("No results found for the specified coordinates.", error);
    }

    [Fact]
    public async Task GetLocationNameAsync_WhenDisplayNameIsNull_ReturnsFailureResult()
    {
        // Arrange
        double latitude = 0;
        double longitude = 0;

        // Create an empty JSON object
        var jsonResponse = "{}";

        // Set up HTTP response
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _sut.GetLocationNameAsync(latitude, longitude);

        // Assert
        var error = result.Errors.FirstOrDefault();
        Assert.False(result.IsSuccess);
        Assert.Equal("No results found for the specified coordinates.", error);
    }

    [Fact]
    public async Task GetLocationNameAsync_WhenHttpRequestFails_ReturnsFailureResult()
    {
        // Arrange
        double latitude = 40.7128;
        double longitude = -74.0060;

        // Set up HTTP response
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = new StringContent("Internal Server Error")
        };
        _httpMessageHandler.ShouldThrowException = true;

        // Act
        var result = await _sut.GetLocationNameAsync(latitude, longitude);

        // Assert
        var error = result.Errors.FirstOrDefault();
        Assert.False(result.IsSuccess);
        Assert.Equal("An error occurred while retrieving location name data.", error);
    }

    [Fact]
    public async Task GetLocationNameAsync_WhenJsonDeserializationFails_ReturnsFailureResult()
    {
        // Arrange
        double latitude = 40.7128;
        double longitude = -74.0060;

        // Set up HTTP response with invalid JSON
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("Invalid JSON", Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _sut.GetLocationNameAsync(latitude, longitude);

        // Assert
        var error = result.Errors.FirstOrDefault();
        Assert.False(result.IsSuccess);
        Assert.Equal("An error occurred while processing location name data.", error);
    }

    [Fact]
    public async Task GetGeolocationAsync_ConstructsProperURL()
    {
        // Arrange
        var location = "New York";
        var geocodeResponses = new List<GeocodeForwardResponse>
        {
            new GeocodeForwardResponse
            {
                DisplayName = "New York City, NY, USA",
                Latitude = 40.7128,
                Longitude = -74.0060
            }
        };

        // Set up HTTP response
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(geocodeResponses), Encoding.UTF8, "application/json")
        };

        // Act
        await _sut.GetGeolocationAsync(location);

        // Assert
        Assert.NotNull(_httpMessageHandler.LastRequestUri);

        // Get the full URL string for inspection
        var requestUrl = _httpMessageHandler.LastRequestUri?.ToString() ?? string.Empty;

        // Verify the URL contains both the query parameter and API key
        Assert.Contains("q=", requestUrl);
        Assert.Contains("New", requestUrl);
        Assert.Contains("York", requestUrl);
        Assert.Contains("api_key=test-api-key", requestUrl);
    }

    [Fact]
    public async Task GetLocationNameAsync_ConstructsProperURL()
    {
        // Arrange
        double latitude = 40.7128;
        double longitude = -74.0060;

        // Create a valid response
        var response = new GeocodeReverseResponse
        {
            DisplayName = "New York City, NY, USA"
        };

        // Set up HTTP response
        _httpMessageHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        };

        // Act
        await _sut.GetLocationNameAsync(latitude, longitude);

        // Assert
        Assert.NotNull(_httpMessageHandler.LastRequestUri);
        var requestUrl = _httpMessageHandler.LastRequestUri?.ToString() ?? string.Empty;

        // Check for required parameters
        Assert.Contains($"lat={latitude}", requestUrl);
        Assert.Contains($"lon={longitude}", requestUrl);
        Assert.Contains("api_key=test-api-key", requestUrl);
    }
}

/// <summary>
/// A test-friendly HTTP message handler that allows controlling HTTP responses
/// </summary>
public class HttpMessageHandlerStub : HttpMessageHandler
{
    public HttpResponseMessage Response { get; set; } = new HttpResponseMessage(HttpStatusCode.OK);
    public Uri? LastRequestUri { get; private set; }
    public bool ShouldThrowException { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;

        if (ShouldThrowException && Response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException("Simulated HTTP error", null, Response.StatusCode);
        }

        return Task.FromResult(Response);
    }
}