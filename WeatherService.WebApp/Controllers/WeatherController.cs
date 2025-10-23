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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Echo(WeatherQueryViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(model.Message))
            {
                model.EchoResult = await apiClient.EchoAsync(model.Message, cancellationToken);
            }
            else
            {
                ModelState.AddModelError(nameof(model.Message), "Please provide a message to echo.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Echo call failed");
            ModelState.AddModelError(string.Empty, "Echo failed: " + ex.Message);
        }

        return View("Index", model);
    }

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
