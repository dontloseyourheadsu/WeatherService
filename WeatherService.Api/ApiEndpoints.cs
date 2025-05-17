namespace WeatherService.Api;

/// <summary>
/// API Endpoints.
/// </summary>
public static class ApiEndpoints
{
    /// <summary>
    /// Base of the API route.
    /// </summary>
    private const string ApiBase = "api";

    /// <summary>
    /// Endpoints related to the ForecastController.
    /// </summary>
    public static class Forecasts
    {
        /// <summary>
        /// Base of the forecast requests.
        /// </summary>
        private const string Base = $"{ApiBase}/forecast";

        /// <summary>
        /// Path to the forecast controller.
        /// </summary>
        public const string Echo = $"{Base}/echo";

        /// <summary>
        /// Path to GET forecast data by coordinates.
        /// </summary>
        public const string GetForecastByCoordinates = $"{Base}/coordinates";

        /// <summary>
        /// Path to GET forecast data by location.
        /// </summary>
        public const string GetForecastByLocation = $"{Base}/location";
    }
}
