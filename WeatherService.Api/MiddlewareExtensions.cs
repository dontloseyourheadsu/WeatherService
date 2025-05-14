namespace WeatherService.Api;

/// <summary>
/// Extensions for middleware registration.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Registers the error handling middleware in the application pipeline.
    /// </summary>
    /// <param name="builder">Builder for configuring the application pipeline.</param>
    /// <returns>Updated application builder with the error handling middleware registered.</returns>
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
