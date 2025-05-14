using WeatherService.Application.Models.OpenMeteo;

namespace WeatherService.Application.Services.OpenMeteo;

/// <summary>
/// Provides helper operations that translate Open-Meteo time-stamped data
/// into the UTC-normalised instants your back-end needs.
/// </summary>
public interface IOpenMeteoForecastTimeService
{
    /// <summary>
    /// Returns the current local time using the response's UTC-offset.
    /// </summary>
    /// <param name="response"> The Open-Meteo forecast response.</param>
    /// <returns>The current local time.</returns>
    DateTime GetCurrentLocalTime(OpenMeteoForecastResponse response);

    /// <summary>
    /// Finds the index of the hourly forecast matching the current hour of the provided local time.
    /// </summary>
    /// <param name="response">The OpenMeteo forecast response</param>
    /// <param name="localTime">The local time to match</param>
    /// <returns>Index of the matching hourly forecast</returns>
    int FindCurrentHourlyIndex(OpenMeteoForecastResponse response, DateTime localTime);

    /// <summary>
    /// Converts the hourly timestamp at <paramref name="hourlyIndex"/> to a normalised UTC
    /// instant (minutes and seconds cleared).
    /// </summary>
    /// <param name="response">The Open-Meteo forecast response.</param>
    /// <param name="hourlyIndex">The index of the hourly forecast.</param>
    /// <returns>The UTC-normalised instant.</returns>
    DateTime NormalizeToUtc(OpenMeteoForecastResponse response, int hourlyIndex);

    /// <summary>
    /// Looks up sunrise for the calendar date represented by <paramref name="normalizedUtc"/>.
    /// </summary>
    /// <param name="response">The Open-Meteo forecast response.</param>
    /// <param name="normalizedUtc">The UTC-normalised date.</param>
    /// <returns>The sunrise time for the specified date.</returns>
    DateTime GetSunriseForDate(OpenMeteoForecastResponse response, DateTime normalizedUtc);
}
