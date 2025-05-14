using WeatherService.Application.Models.OpenMeteo;

namespace WeatherService.Application.Services.OpenMeteo;

/// <inheritdoc />
internal sealed class OpenMeteoForecastTimeService : IOpenMeteoForecastTimeService
{
    /// <inheritdoc />
    public DateTime GetCurrentLocalTime(OpenMeteoForecastResponse response)
    {
        var offset = TimeSpan.FromSeconds(response.UtcOffsetSeconds);
        return DateTime.UtcNow + offset;
    }

    /// <inheritdoc />
    public int FindCurrentHourlyIndex(OpenMeteoForecastResponse response, DateTime localTime)
    {
        // Get just the date part and add the current hour to create a reference time
        DateTime targetHour = localTime.Date.AddHours(localTime.Hour);

        // Find the index where the hour matches our target hour
        return response.Hourly.Time
            .Select((t, i) => new { Index = i, Time = DateTime.Parse(t) })
            .FirstOrDefault(x => x.Time.Hour == targetHour.Hour && x.Time.Date == targetHour.Date)?.Index ?? -1;
    }

    /// <inheritdoc />
    public DateTime NormalizeToUtc(OpenMeteoForecastResponse response, int hourlyIndex)
    {
        var offset = TimeSpan.FromSeconds(response.UtcOffsetSeconds);
        var localTs = DateTime.Parse(response.Hourly.Time[hourlyIndex]);
        var utc = localTs - offset;
        return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc);
    }

    /// <inheritdoc />
    public DateTime GetSunriseForDate(OpenMeteoForecastResponse response, DateTime normalizedUtc)
    {
        var offset = TimeSpan.FromSeconds(response.UtcOffsetSeconds);
        var localDate = (normalizedUtc + offset).Date;

        // Find the sunrise time for the specified date
        // d represents the date in the response
        // i represents the index of the sunrise time
        // x represents the date and sunrise time for the specified date
        return response.Daily.Time
               .Select((d, i) => new { Date = DateTime.Parse(d), Sunrise = DateTime.Parse(response.Daily.Sunrise[i]) })
               .FirstOrDefault(x => x.Date.Date == localDate)?.Sunrise
               ?? localDate;
    }
}