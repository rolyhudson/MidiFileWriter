using WeatherDrums.Models;

namespace WeatherDrums.Mapping;

/// <summary>
/// Maps weather data to drum patterns
/// </summary>
public class WeatherToDrumMapper
{
    // Ticks per quarter note (standard MIDI resolution)
    public const int TicksPerQuarterNote = 480;
    
    // One bar = 4 quarter notes
    public const int TicksPerBar = TicksPerQuarterNote * 4;
    
    // 16th note resolution for hi-hats
    public const int TicksPer16th = TicksPerQuarterNote / 4;
    
    // 8th note resolution
    public const int TicksPer8th = TicksPerQuarterNote / 2;

    /// <summary>
    /// Number of bars to generate per hour of weather data
    /// </summary>
    public int BarsPerHour { get; set; } = 4;

    /// <summary>
    /// Converts a list of hourly weather points into drum patterns
    /// </summary>
    public List<DrumPattern> MapWeatherToDrums(List<HourlyWeatherPoint> weatherPoints)
    {
        var patterns = new List<DrumPattern>();

        // Calculate min/max values for normalization
        var tempMin = weatherPoints.Min(p => p.Temperature);
        var tempMax = weatherPoints.Max(p => p.Temperature);
        var windMax = weatherPoints.Max(p => p.WindSpeed);
        var precipMax = Math.Max(weatherPoints.Max(p => p.Precipitation), 0.1); // Avoid div by zero

        foreach (var weather in weatherPoints)
        {
            // Generate multiple bars per hour
            for (int bar = 0; bar < BarsPerHour; bar++)
            {
                var pattern = CreatePatternForWeather(
                    weather, 
                    tempMin, tempMax, 
                    windMax, 
                    precipMax,
                    bar);
                
                pattern.SourceWeather = weather;
                patterns.Add(pattern);
            }
        }

        return patterns;
    }

    private DrumPattern CreatePatternForWeather(
        HourlyWeatherPoint weather,
        double tempMin, double tempMax,
        double windMax,
        double precipMax,
        int barIndex)
    {
        var pattern = new DrumPattern { LengthTicks = TicksPerBar };

        // Normalize weather values to 0-1 range
        double tempNorm = tempMax > tempMin 
            ? (weather.Temperature - tempMin) / (tempMax - tempMin) 
            : 0.5;
        double windNorm = windMax > 0 ? weather.WindSpeed / windMax : 0;
        double humidityNorm = weather.Humidity / 100.0;
        double precipNorm = weather.Precipitation / precipMax;

        // Add kick pattern based on temperature
        AddKickPattern(pattern, tempNorm, barIndex);
        
        // Add hi-hat pattern based on wind speed
        AddHiHatPattern(pattern, windNorm);
        
        // Add snare pattern based on humidity
        AddSnarePattern(pattern, humidityNorm, barIndex);
        
        // Add cymbal crashes based on precipitation
        AddCymbalPattern(pattern, precipNorm, barIndex);

        return pattern;
    }

    /// <summary>
    /// Higher temperature = more kick hits (busier pattern)
    /// </summary>
    private void AddKickPattern(DrumPattern pattern, double tempNorm, int barIndex)
    {
        // Base kick on beats 1 and 3
        int baseVelocity = 80 + (int)(tempNorm * 40); // 80-120
        
        // Beat 1 - always
        pattern.Hits.Add(new DrumHit(DrumNotes.Kick, 0, baseVelocity));
        
        // Beat 3 - always
        pattern.Hits.Add(new DrumHit(DrumNotes.Kick, TicksPerQuarterNote * 2, baseVelocity - 10));

        // Higher temperature adds more kick variations
        if (tempNorm > 0.3)
        {
            // Add kick on "and" of 2
            pattern.Hits.Add(new DrumHit(DrumNotes.Kick, TicksPerQuarterNote + TicksPer8th, baseVelocity - 20));
        }
        
        if (tempNorm > 0.6)
        {
            // Add kick on "and" of 4
            pattern.Hits.Add(new DrumHit(DrumNotes.Kick, TicksPerQuarterNote * 3 + TicksPer8th, baseVelocity - 20));
        }

        if (tempNorm > 0.8 && barIndex % 2 == 1)
        {
            // Double kick fill on alternate bars
            pattern.Hits.Add(new DrumHit(DrumNotes.Kick, TicksPerQuarterNote * 3 + TicksPer16th * 3, baseVelocity - 15));
        }
    }

    /// <summary>
    /// Higher wind speed = busier hi-hat pattern (8ths to 16ths)
    /// </summary>
    private void AddHiHatPattern(DrumPattern pattern, double windNorm)
    {
        int velocity = 60 + (int)(windNorm * 40); // 60-100
        
        // Determine hi-hat density based on wind
        int divisions = windNorm > 0.5 ? 16 : 8; // 16th notes for high wind, 8th notes otherwise
        int ticksPerDivision = TicksPerBar / divisions;
        
        for (int i = 0; i < divisions; i++)
        {
            long position = i * ticksPerDivision;
            
            // Alternate between closed and open hi-hat for variety
            int note = DrumNotes.ClosedHiHat;
            int hitVelocity = velocity;
            
            // Open hi-hat on off-beats when wind is moderate-high
            if (windNorm > 0.4 && i % 4 == 2)
            {
                note = DrumNotes.OpenHiHat;
                hitVelocity = velocity + 10;
            }
            
            // Accent pattern - stronger on beats
            if (i % (divisions / 4) == 0)
            {
                hitVelocity = Math.Min(127, hitVelocity + 15);
            }
            
            pattern.Hits.Add(new DrumHit(note, position, hitVelocity));
        }
    }

    /// <summary>
    /// Higher humidity = more snare fills and ghost notes
    /// </summary>
    private void AddSnarePattern(DrumPattern pattern, double humidityNorm, int barIndex)
    {
        int baseVelocity = 90 + (int)(humidityNorm * 30); // 90-120
        
        // Standard backbeat on 2 and 4
        pattern.Hits.Add(new DrumHit(DrumNotes.Snare, TicksPerQuarterNote, baseVelocity));
        pattern.Hits.Add(new DrumHit(DrumNotes.Snare, TicksPerQuarterNote * 3, baseVelocity));
        
        // Add ghost notes based on humidity
        if (humidityNorm > 0.4)
        {
            // Ghost note before beat 2
            pattern.Hits.Add(new DrumHit(DrumNotes.Snare, TicksPerQuarterNote - TicksPer16th, 40));
        }
        
        if (humidityNorm > 0.6)
        {
            // Ghost note before beat 4
            pattern.Hits.Add(new DrumHit(DrumNotes.Snare, TicksPerQuarterNote * 3 - TicksPer16th, 40));
        }
        
        if (humidityNorm > 0.75 && barIndex % 4 == 3)
        {
            // Snare fill on every 4th bar
            pattern.Hits.Add(new DrumHit(DrumNotes.Snare, TicksPerQuarterNote * 3 + TicksPer8th, 70));
            pattern.Hits.Add(new DrumHit(DrumNotes.Snare, TicksPerQuarterNote * 3 + TicksPer8th + TicksPer16th, 65));
        }
    }

    /// <summary>
    /// Precipitation triggers cymbal crashes
    /// </summary>
    private void AddCymbalPattern(DrumPattern pattern, double precipNorm, int barIndex)
    {
        if (precipNorm <= 0.01) return; // No rain, no cymbals
        
        int velocity = 70 + (int)(precipNorm * 50); // 70-120
        
        // Crash on first beat of first bar in each hour segment
        if (barIndex == 0)
        {
            pattern.Hits.Add(new DrumHit(DrumNotes.CrashCymbal, 0, velocity));
        }
        
        // More crashes with heavier precipitation
        if (precipNorm > 0.3 && barIndex == 2)
        {
            pattern.Hits.Add(new DrumHit(DrumNotes.CrashCymbal, 0, velocity - 10));
        }
        
        // Ride cymbal adds texture during rain
        if (precipNorm > 0.5)
        {
            for (int i = 0; i < 4; i++)
            {
                pattern.Hits.Add(new DrumHit(DrumNotes.RideCymbal, i * TicksPerQuarterNote, 50 + (int)(precipNorm * 30)));
            }
        }
    }
}
