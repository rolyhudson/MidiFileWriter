using System.Text.Json;
using WeatherDrums.Models;

namespace WeatherDrums.Services;

/// <summary>
/// Service for fetching weather data from Open-Meteo API
/// </summary>
public class WeatherService : IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

    public WeatherService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Fetches hourly weather data for the specified location
    /// </summary>
    /// <param name="latitude">Latitude of the location</param>
    /// <param name="longitude">Longitude of the location</param>
    /// <param name="hoursAhead">Number of hours of forecast to retrieve (default 48)</param>
    /// <returns>List of hourly weather data points</returns>
    public async Task<List<HourlyWeatherPoint>> GetHourlyWeatherAsync(
        double latitude, 
        double longitude, 
        int hoursAhead = 48)
    {
        var url = BuildUrl(latitude, longitude, hoursAhead);
        
        Console.WriteLine($"Fetching weather data from Open-Meteo API...");
        Console.WriteLine($"Location: {latitude:F4}, {longitude:F4}");
        
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var weatherResponse = JsonSerializer.Deserialize<WeatherResponse>(json);
            
            if (weatherResponse?.Hourly == null)
            {
                throw new InvalidOperationException("Invalid response from weather API");
            }

            var points = HourlyWeatherPoint.FromHourlyData(weatherResponse.Hourly);
            
            // Limit to requested hours
            points = points.Take(hoursAhead).ToList();
            
            Console.WriteLine($"Retrieved {points.Count} hours of weather data");
            PrintWeatherSummary(points);
            
            return points;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error fetching weather data: {ex.Message}");
            throw;
        }
    }

    private static string BuildUrl(double latitude, double longitude, int hoursAhead)
    {
        // Open-Meteo API parameters
        var parameters = new Dictionary<string, string>
        {
            ["latitude"] = latitude.ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
            ["longitude"] = longitude.ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
            ["hourly"] = "temperature_2m,relative_humidity_2m,precipitation,wind_speed_10m",
            ["forecast_hours"] = hoursAhead.ToString()
        };

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
        return $"{BaseUrl}?{queryString}";
    }

    private static void PrintWeatherSummary(List<HourlyWeatherPoint> points)
    {
        if (points.Count == 0) return;

        var avgTemp = points.Average(p => p.Temperature);
        var maxWind = points.Max(p => p.WindSpeed);
        var totalPrecip = points.Sum(p => p.Precipitation);
        var avgHumidity = points.Average(p => p.Humidity);

        Console.WriteLine($"Weather Summary:");
        Console.WriteLine($"  Temperature: {avgTemp:F1}°C average");
        Console.WriteLine($"  Wind: up to {maxWind:F1} km/h");
        Console.WriteLine($"  Precipitation: {totalPrecip:F1} mm total");
        Console.WriteLine($"  Humidity: {avgHumidity:F0}% average");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
