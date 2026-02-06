using System.Text.Json.Serialization;

namespace WeatherDrums.Models;

/// <summary>
/// Root response from Open-Meteo API
/// </summary>
public class WeatherResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("hourly")]
    public HourlyData? Hourly { get; set; }
}

/// <summary>
/// Hourly weather data arrays from Open-Meteo
/// </summary>
public class HourlyData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m")]
    public List<double> Temperature { get; set; } = new();

    [JsonPropertyName("relative_humidity_2m")]
    public List<int> Humidity { get; set; } = new();

    [JsonPropertyName("precipitation")]
    public List<double> Precipitation { get; set; } = new();

    [JsonPropertyName("wind_speed_10m")]
    public List<double> WindSpeed { get; set; } = new();
}

/// <summary>
/// Represents a single hour of weather data (flattened for easier processing)
/// </summary>
public class HourlyWeatherPoint
{
    public DateTime Time { get; set; }
    public double Temperature { get; set; }  // Celsius
    public int Humidity { get; set; }         // Percentage (0-100)
    public double Precipitation { get; set; } // mm
    public double WindSpeed { get; set; }     // km/h

    public static List<HourlyWeatherPoint> FromHourlyData(HourlyData data)
    {
        var points = new List<HourlyWeatherPoint>();
        
        for (int i = 0; i < data.Time.Count; i++)
        {
            points.Add(new HourlyWeatherPoint
            {
                Time = DateTime.Parse(data.Time[i]),
                Temperature = i < data.Temperature.Count ? data.Temperature[i] : 0,
                Humidity = i < data.Humidity.Count ? data.Humidity[i] : 50,
                Precipitation = i < data.Precipitation.Count ? data.Precipitation[i] : 0,
                WindSpeed = i < data.WindSpeed.Count ? data.WindSpeed[i] : 0
            });
        }

        return points;
    }
}
