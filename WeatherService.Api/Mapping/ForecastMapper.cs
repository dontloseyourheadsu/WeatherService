using System.Linq;
using WeatherService.Application.Models;
using WeatherService.Contracts.Responses;

namespace WeatherService.Api.Mapping;

/// <summary>
/// Maps forecast data between different representations.
/// </summary>
public static class ForecastMapper
{
    /// <summary>
    /// Maps a <see cref="ForecastDetails"/> object to a <see cref="ForecastDetailsResponse"/> object.
    /// </summary>
    /// <param name="forecastDetails">Forecast details to map.</param>
    /// <returns>Forecast details response.</returns>
    public static ForecastDetailsResponse ToForecastDetailsResponse(this ForecastDetails forecastDetails)
    {
        return new ForecastDetailsResponse
        {
            Latitude = forecastDetails.Latitude,
            Longitude = forecastDetails.Longitude,
            Temperature = forecastDetails.Temperature,
            WindDirection = forecastDetails.WindDirection,
            WindSpeed = forecastDetails.WindSpeed,
            Sunrise = forecastDetails.Sunrise,
            Units = new ForecastDetailsUnitsResponse
            {
                TemperatureUnit = forecastDetails.TemperatureUnit,
                WindDirectionUnit = forecastDetails.WindDirectionUnit,
                WindSpeedUnit = forecastDetails.WindSpeedUnit
            }
        };
    }

    /// <summary>
    /// Maps a <see cref="ZoneAnalytics"/> domain model to a <see cref="ZoneAnalyticsResponse"/> contract response.
    /// </summary>
    public static ZoneAnalyticsResponse ToZoneAnalyticsResponse(this ZoneAnalytics zoneAnalytics)
    {
        return new ZoneAnalyticsResponse
        {
            ZoneName = zoneAnalytics.ZoneName,
            Points = zoneAnalytics.Points.Select(p => new WeatherService.Contracts.Responses.ZonePoint
            {
                Latitude = p.Latitude,
                Longitude = p.Longitude
            }).ToList(),
            History = zoneAnalytics.History.Select(h => new WeatherService.Contracts.Responses.HistoricalRecord
            {
                Timestamp = h.Timestamp,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
                Temperature = h.Temperature,
                WindSpeed = h.WindSpeed,
                WindDirection = h.WindDirection
            }).ToList()
        };
    }
}
