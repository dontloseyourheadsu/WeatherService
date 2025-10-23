using Microsoft.AspNetCore.Mvc;
using WeatherService.Contracts.Responses;
using WeatherService.WebApp.Services;
using WeatherService.WebApp.Models;

namespace WeatherService.WebApp.Controllers;

public class WeatherController(IForecastApiClient apiClient, ILogger<WeatherController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new WeatherQueryViewModel());
    }

    // Echo endpoint removed from UI; keeping code clean by removing action.

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ByCoordinates(WeatherQueryViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            model.ForecastByCoordinates = await apiClient.GetForecastByCoordinatesAsync(model.Latitude, model.Longitude, cancellationToken);
            if (model.ForecastByCoordinates is null)
            {
                ModelState.AddModelError(string.Empty, "No forecast found for the specified coordinates.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Forecast by coordinates failed");
            ModelState.AddModelError(string.Empty, "Request failed: " + ex.Message);
        }

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ByLocation(WeatherQueryViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(model.Location))
            {
                model.ForecastByLocation = await apiClient.GetForecastByLocationAsync(model.Location, cancellationToken);
                if (model.ForecastByLocation is null)
                {
                    ModelState.AddModelError(string.Empty, "No forecast found for the specified location.");
                }
            }
            else
            {
                ModelState.AddModelError(nameof(model.Location), "Please provide a location.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Forecast by location failed");
            ModelState.AddModelError(string.Empty, "Request failed: " + ex.Message);
        }

        return View("Index", model);
    }
}
