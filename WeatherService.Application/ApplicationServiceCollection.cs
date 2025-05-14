using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WeatherService.Application.Data;
using WeatherService.Application.Models.Options;
using WeatherService.Application.Repositories;
using WeatherService.Application.Services;
using WeatherService.Application.Services.OpenMeteo;

namespace WeatherService.Application;

/// <summary>
/// ApplicationServiceCollection is a static class that provides extension methods for registering application services.
/// </summary>
public static class ApplicationServiceCollection
{
    /// <summary>
    /// Extension method to add application services to the service collection.
    /// </summary>
    /// <param name="services">Service collection to add services to.</param>
    /// <returns>Service collection with added services.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register the OpenMeteo services
        services.AddHttpClient("OpenMeteo", (serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<OpenMeteoSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutInSeconds);
        });
        services.AddScoped<IOpenMeteoWeatherForecastService, OpenMeteoWeatherForecastService>();

        // Register the MongoDB services
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionUri);
        });
        services.AddScoped<IWeatherMongoDatabaseFactory, WeatherMongoDatabaseFactory>();

        // Register the repository
        services.AddScoped<IMongoDbForecastRepository, MongoDbForecastRepository>();

        // Register the forecast services
        services.AddScoped<IOpenMeteoForecastTimeService, OpenMeteoForecastTimeService>();
        services.AddScoped<IForecastService, ForecastService>();

        return services;
    }
}
