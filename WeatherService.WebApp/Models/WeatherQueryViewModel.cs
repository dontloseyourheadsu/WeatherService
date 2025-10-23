using System.ComponentModel.DataAnnotations;
using WeatherService.Contracts.Responses;

namespace WeatherService.WebApp.Models;

public class WeatherQueryViewModel
{
    // Coordinates
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    public ForecastDetailsResponse? ForecastByCoordinates { get; set; }

    // Location
    [Display(Name = "Location (city, region, country)")]
    public string? Location { get; set; }

    public ForecastDetailsResponse? ForecastByLocation { get; set; }
}
