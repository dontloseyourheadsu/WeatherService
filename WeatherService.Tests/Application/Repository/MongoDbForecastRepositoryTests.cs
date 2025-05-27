using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Connections;
using NSubstitute;
using Testcontainers.MongoDb;
using WeatherService.Application.Data;
using WeatherService.Application.Models.MongoDb;
using WeatherService.Application.Models.Options;
using WeatherService.Application.Repositories;
using WeatherService.Application.Utilities;

namespace WeatherService.Tests.Application.Repositories;

public class MongoDbForecastRepositoryTests : IAsyncLifetime
{
    // MongoDB Testcontainer
    private readonly MongoDbContainer _mongoDbContainer;

    // Settings for MongoDB connection
    private MongoDbSettings _mongoDbSettings = null!;

    // MongoDB client for direct access to the database
    private IMongoClient _mongoClient = null!;

    // Database factory for the repository
    private IWeatherMongoDatabaseFactory _databaseFactory = null!;

    // Logger for testing
    private ILogger<MongoDbForecastRepository> _logger = null!;

    // Repository being tested
    private MongoDbForecastRepository _repository = null!;

    // Services provider for DI
    private ServiceProvider _serviceProvider = null!;

    // Test data
    private readonly DateTime _testTimestamp = new DateTime(2025, 5, 20, 10, 0, 0, DateTimeKind.Utc);
    private readonly double _testLatitude = 35.689487;
    private readonly double _testLongitude = 139.691711;
    private readonly DateTime _testSunrise = new DateTime(2025, 5, 20, 5, 30, 0, DateTimeKind.Utc);

    public MongoDbForecastRepositoryTests()
    {
        // Create MongoDB container
        _mongoDbContainer = new MongoDbBuilder()
            .WithImage("mongo:6.0")
            .WithPortBinding(27017, true)
            .WithUsername(string.Empty)
            .WithPassword(string.Empty)
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start the container
        await _mongoDbContainer.StartAsync();

        // Configure services
        var services = new ServiceCollection();

        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Set up MongoDB settings with dynamic connection string from container
        _mongoDbSettings = new MongoDbSettings
        {
            ConnectionUri = _mongoDbContainer.GetConnectionString(),
            DatabaseName = "WeatherTestDb",
            Collections = new MongoDbCollectionsSettings
            {
                Forecasts = "TestForecasts"
            }
        };

        // Configure services
        services.AddSingleton(Options.Create(_mongoDbSettings));

        // Add MongoDB Client
        services.AddSingleton<IMongoClient>(new MongoClient(_mongoDbSettings.ConnectionUri));

        // Add database factory
        services.AddSingleton<IWeatherMongoDatabaseFactory, WeatherMongoDatabaseFactory>();

        // Add logger
        services.AddLogging();

        // Build the service provider
        _serviceProvider = services.BuildServiceProvider();

        // Get required services
        _mongoClient = _serviceProvider.GetRequiredService<IMongoClient>();
        _databaseFactory = _serviceProvider.GetRequiredService<IWeatherMongoDatabaseFactory>();
        _logger = _serviceProvider.GetRequiredService<ILogger<MongoDbForecastRepository>>();

        // Create repository
        _repository = new MongoDbForecastRepository(_databaseFactory, Options.Create(_mongoDbSettings), _logger);
    }

    public async Task DisposeAsync()
    {
        // Stop container
        await _mongoDbContainer.DisposeAsync();

        // Dispose service provider
        await _serviceProvider.DisposeAsync();
    }

    // Helper method to create a test forecast
    private MongoDbForecast CreateTestForecast()
    {
        return new MongoDbForecast
        {
            Timestamp = _testTimestamp,
            Latitude = _testLatitude,
            Longitude = _testLongitude,
            Temperature = 15.5,
            TemperatureUnit = "°C",
            WindSpeed = 5.2,
            WindSpeedUnit = "km/h",
            WindDirection = 180,
            WindDirectionUnit = "°",
            Sunrise = _testSunrise
        };
    }

    [Fact]
    public async Task InsertAsync_ShouldInsertForecastSuccessfully()
    {
        // Arrange
        var forecast = CreateTestForecast();

        // Act
        var result = await _repository.InsertAsync(forecast);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);

        // Verify forecast exists in database
        var database = _mongoClient.GetDatabase(_mongoDbSettings.DatabaseName);
        var collection = database.GetCollection<MongoDbForecast>(_mongoDbSettings.Collections.Forecasts);
        var filter = Builders<MongoDbForecast>.Filter.And(
            Builders<MongoDbForecast>.Filter.Eq(f => f.Timestamp, forecast.Timestamp),
            Builders<MongoDbForecast>.Filter.Eq(f => f.Latitude, forecast.Latitude),
            Builders<MongoDbForecast>.Filter.Eq(f => f.Longitude, forecast.Longitude)
        );
        var storedForecast = await collection.Find(filter).FirstOrDefaultAsync();

        Assert.NotNull(storedForecast);
        Assert.NotNull(storedForecast.Id);
        Assert.Equal(forecast.Temperature, storedForecast.Temperature);
        Assert.Equal(forecast.WindSpeed, storedForecast.WindSpeed);
        Assert.Equal(forecast.WindDirection, storedForecast.WindDirection);
    }

    [Fact]
    public async Task GetByTimeAndLocationAsync_WhenForecastExists_ShouldReturnForecast()
    {
        // Arrange
        var forecast = CreateTestForecast();

        // Insert forecast directly using MongoDB client
        var database = _mongoClient.GetDatabase(_mongoDbSettings.DatabaseName);
        var collection = database.GetCollection<MongoDbForecast>(_mongoDbSettings.Collections.Forecasts);
        await collection.InsertOneAsync(forecast);

        // Act
        var result = await _repository.GetByTimeAndLocationAsync(
            forecast.Timestamp,
            forecast.Latitude,
            forecast.Longitude);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Value);

        Assert.Equal(forecast.Timestamp, result.Value.Timestamp);
        Assert.Equal(forecast.Latitude, result.Value.Latitude);
        Assert.Equal(forecast.Longitude, result.Value.Longitude);
        Assert.Equal(forecast.Temperature, result.Value.Temperature);
        Assert.Equal(forecast.TemperatureUnit, result.Value.TemperatureUnit);
        Assert.Equal(forecast.WindSpeed, result.Value.WindSpeed);
        Assert.Equal(forecast.WindSpeedUnit, result.Value.WindSpeedUnit);
        Assert.Equal(forecast.WindDirection, result.Value.WindDirection);
        Assert.Equal(forecast.WindDirectionUnit, result.Value.WindDirectionUnit);
        Assert.Equal(
            forecast.Sunrise.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            result.Value.Sunrise.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")
        );
    }

    [Fact]
    public async Task GetByTimeAndLocationAsync_WhenForecastDoesNotExist_ShouldReturnFailure()
    {
        // Act
        var result = await _repository.GetByTimeAndLocationAsync(
            DateTime.UtcNow,
            0.0,
            0.0);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Forecast not found", result.Errors.First());
    }

    [Fact]
    public async Task UpdateAsync_WhenForecastExists_ShouldUpdateSuccessfully()
    {
        // Arrange
        var forecast = CreateTestForecast();

        // Insert forecast directly
        var database = _mongoClient.GetDatabase(_mongoDbSettings.DatabaseName);
        var collection = database.GetCollection<MongoDbForecast>(_mongoDbSettings.Collections.Forecasts);
        await collection.InsertOneAsync(forecast);

        // Get the generated ID
        var filter = Builders<MongoDbForecast>.Filter.And(
            Builders<MongoDbForecast>.Filter.Eq(f => f.Timestamp, forecast.Timestamp),
            Builders<MongoDbForecast>.Filter.Eq(f => f.Latitude, forecast.Latitude),
            Builders<MongoDbForecast>.Filter.Eq(f => f.Longitude, forecast.Longitude)
        );
        var insertedForecast = await collection.Find(filter).FirstAsync();

        // Update forecast values
        insertedForecast.Temperature = 20.5;
        insertedForecast.WindSpeed = 8.3;
        insertedForecast.WindDirection = 220;

        // Act
        var result = await _repository.UpdateAsync(insertedForecast);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);

        // Verify forecast was updated in database
        var updatedForecast = await collection.Find(filter).FirstAsync();
        Assert.Equal(20.5, updatedForecast.Temperature);
        Assert.Equal(8.3, updatedForecast.WindSpeed);
        Assert.Equal(220, updatedForecast.WindDirection);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidForecast_ShouldReturnFailure()
    {
        // Arrange - Use a forecast with an invalid ObjectId format
        var forecast = CreateTestForecast();
        forecast.Id = "invalid_id_format"; // Invalid MongoDB ObjectId format

        // Act
        var result = await _repository.UpdateAsync(forecast);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GetByTimeAndLocationAsync_WithCancellation_ShouldHandleCancelledToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var task = _repository.GetByTimeAndLocationAsync(
            DateTime.UtcNow,
            0.0,
            0.0,
            cts.Token);

        // Assert
        var completedTask = await Task.WhenAny(task, Task.Delay(5000)); // 5-second timeout
        Assert.Equal(task, completedTask);
    }
}