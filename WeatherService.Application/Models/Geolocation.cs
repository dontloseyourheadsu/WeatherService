namespace WeatherService.Application.Models
{
    internal class Geolocation
    {
        /// <summary>
        /// Gets or sets the latitude of the location.
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// Gets or sets the longitude of the location.
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// Gets or sets the display name of the location.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }
}
