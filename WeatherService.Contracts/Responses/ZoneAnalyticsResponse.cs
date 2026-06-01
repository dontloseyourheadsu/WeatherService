using System;
using System.Collections.Generic;

namespace WeatherService.Contracts.Responses
{
    public class ZonePoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class HistoricalRecord
    {
        public DateTime Timestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Temperature { get; set; }
        public double WindSpeed { get; set; }
        public int WindDirection { get; set; }
    }

    public class ZoneAnalyticsResponse
    {
        public string ZoneName { get; set; } = string.Empty;
        public List<ZonePoint> Points { get; set; } = new();
        public List<HistoricalRecord> History { get; set; } = new();
    }
}
