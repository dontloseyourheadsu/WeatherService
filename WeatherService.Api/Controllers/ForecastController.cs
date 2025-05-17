using Microsoft.AspNetCore.Mvc;
using WeatherService.Api.Mapping;
using WeatherService.Application.Services;
using WeatherService.Contracts.Responses;
using WeatherService.Contracts.Responses.Errors;

namespace WeatherService.Api.Controllers;

[ApiController]
public class ForecastController : ControllerBase
{
    /// <summary>
    /// The logger instance for logging information and errors.
    /// </summary>
    private readonly ILogger<ForecastController> _logger;

    /// <summary>
    /// The forecast service instance for handling forecast-related operations.
    /// </summary>
    private readonly IForecastService _forecastService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastController"/> class.
    /// </summary>
    /// <param name="forecastService">Forecast service instance for handling forecast-related operations.</param>
    /// <param name="logger">Logger instance for logging information and errors.</param>
    public ForecastController(IForecastService forecastService, ILogger<ForecastController> logger)
    {
        _forecastService = forecastService;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to check if the API is running.
    /// Helps consumers verify the API is up and running.
    /// Echoes the provided message back to the client.
    /// </summary>
    /// <param name="message">Message to echo.</param>
    /// <returns>An IActionResult containing the echoed message.</returns>
    [HttpGet(ApiEndpoints.Forecasts.Echo)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult Echo([FromQuery] string message)
    {
        return Ok(message);
    }

    /// <summary>
    /// Gets the weather forecast for a given latitude and longitude.
    /// Updates values every hour.
    /// </summary>
    /// <param name="latitude">Latitude of the location.</param>
    /// <param name="longitude">Longitude of the location.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An IActionResult containing the forecast details or an error response.</returns>
    [HttpGet(ApiEndpoints.Forecasts.GetForecastByCoordinates)]
    [ProducesResponseType(typeof(ForecastDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetForecastByCoordinates([FromQuery] double latitude, [FromQuery] double longitude, CancellationToken cancellationToken)
    {
        // Validate the latitude and longitude values
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            var errorResponse = new ErrorResponse
            {
                Errors = new List<string> { "Invalid latitude or longitude values." }
            };
            return BadRequest(errorResponse);
        }

        try
        {
            // Validate the input parameters    
            var forecastDetailsResult = await _forecastService.GetWeatherForecastAsync(latitude, longitude, cancellationToken);

            // Check if the result is successful
            if (forecastDetailsResult.IsSuccess)
            {
                // Map the result to the ForecastDetailsResponse
                var forecastDetailsResponse = forecastDetailsResult.Value.ToForecastDetailsResponse();
                return Ok(forecastDetailsResponse);
            }

            // Log the errors if the result is not successful
            var errors = forecastDetailsResult.Errors.ToList();
            _logger.LogWarning("Failed to fetch forecast: {Errors}", errors);

            // Return a 422 Unprocessable Entity response with the error details
            var errorResponse = new ErrorResponse
            {
                Errors = errors
            };
            return StatusCode(StatusCodes.Status422UnprocessableEntity, errorResponse);
        }
        catch (Exception ex)
        {
            // Log the exception details
            _logger.LogError(ex, "An unexpected error occurred while fetching the forecast.");

            // Return a 500 Internal Server Error response with a generic error message
            var errorResponse = new ErrorResponse
            {
                Errors = new List<string> { "An unexpected error occurred." }
            };
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }

    /// <summary>
    /// Gets the weather forecast for a given location name.
    /// Updates values every hour.
    /// </summary>
    /// <param name="location">Location name (city, region, country).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>An IActionResult containing the forecast details or an error response.</returns>
    [HttpGet(ApiEndpoints.Forecasts.GetForecastByLocation)]
    [ProducesResponseType(typeof(ForecastDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetForecastByLocation([FromQuery] string location, CancellationToken cancellationToken)
    {
        // Validate the location parameter
        if (string.IsNullOrWhiteSpace(location))
        {
            var errorResponse = new ErrorResponse
            {
                Errors = new List<string> { "Location cannot be null or empty." }
            };
            return BadRequest(errorResponse);
        }

        try
        {
            // Validate the input parameters    
            var forecastDetailsResult = await _forecastService.GetWeatherForecastAsync(location, cancellationToken);
            // Check if the result is successful
            if (forecastDetailsResult.IsSuccess)
            {
                // Map the result to the ForecastDetailsResponse
                var forecastDetailsResponse = forecastDetailsResult.Value.ToForecastDetailsResponse();
                return Ok(forecastDetailsResponse);
            }
            // Log the errors if the result is not successful
            var errors = forecastDetailsResult.Errors.ToList();
            _logger.LogWarning("Failed to fetch forecast: {Errors}", errors);
            // Return a 422 Unprocessable Entity response with the error details
            var errorResponse = new ErrorResponse
            {
                Errors = errors
            };
            return StatusCode(StatusCodes.Status422UnprocessableEntity, errorResponse);
        }
        catch (Exception ex)
        {
            // Log the exception details
            _logger.LogError(ex, "An unexpected error occurred while fetching the forecast.");
            // Return a 500 Internal Server Error response with a generic error message
            var errorResponse = new ErrorResponse
            {
                Errors = new List<string> { "An unexpected error occurred." }
            };
            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }
}
