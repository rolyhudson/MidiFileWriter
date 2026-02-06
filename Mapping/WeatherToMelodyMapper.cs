using WeatherDrums.Models;

namespace WeatherDrums.Mapping;

/// <summary>
/// Maps weather data to ambient melody patterns
/// </summary>
public class WeatherToMelodyMapper
{
    // Use same timing constants as drum mapper for synchronization
    public const int TicksPerQuarterNote = WeatherToDrumMapper.TicksPerQuarterNote;
    public const int TicksPerBar = WeatherToDrumMapper.TicksPerBar;
    public const int TicksPerWholeNote = TicksPerQuarterNote * 4;
    public const int TicksPerHalfNote = TicksPerQuarterNote * 2;
    public const int TicksPer8th = TicksPerQuarterNote / 2;
    public const int TicksPer16th = TicksPerQuarterNote / 4;

    /// <summary>
    /// Number of bars to generate per hour of weather data
    /// </summary>
    public int BarsPerHour { get; set; } = 4;

    /// <summary>
    /// MIDI channel for melody (0-15, default 0 = channel 1)
    /// </summary>
    public int MelodyChannel { get; set; } = 0;

    /// <summary>
    /// Base root note (default C3 = 48 for ambient bass register)
    /// </summary>
    public int BaseRootNote { get; set; } = 48;

    private readonly Random _random = new();

    /// <summary>
    /// Converts a list of hourly weather points into melody patterns
    /// </summary>
    public List<MelodyPattern> MapWeatherToMelody(List<HourlyWeatherPoint> weatherPoints)
    {
        var patterns = new List<MelodyPattern>();

        // Calculate min/max values for normalization
        var tempMin = weatherPoints.Min(p => p.Temperature);
        var tempMax = weatherPoints.Max(p => p.Temperature);
        var windMax = Math.Max(weatherPoints.Max(p => p.WindSpeed), 1);
        var precipMax = Math.Max(weatherPoints.Max(p => p.Precipitation), 0.1);

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

    private MelodyPattern CreatePatternForWeather(
        HourlyWeatherPoint weather,
        double tempMin, double tempMax,
        double windMax,
        double precipMax,
        int barIndex)
    {
        // Select scale based on temperature
        var scale = Scales.SelectScaleByTemperature(weather.Temperature);
        
        // Determine octave based on time of day (night = lower, day = higher)
        int octaveShift = GetOctaveShiftFromTime(weather.Time);
        int rootNote = BaseRootNote + (octaveShift * 12);

        var pattern = new MelodyPattern
        {
            LengthTicks = TicksPerBar,
            Scale = scale,
            RootNote = rootNote
        };

        // Normalize weather values
        double tempNorm = tempMax > tempMin
            ? (weather.Temperature - tempMin) / (tempMax - tempMin)
            : 0.5;
        double windNorm = weather.WindSpeed / windMax;
        double humidityNorm = weather.Humidity / 100.0;
        double precipNorm = weather.Precipitation / precipMax;

        // Generate ambient textures based on weather
        AddPadNotes(pattern, scale, rootNote, humidityNorm, tempNorm);
        AddArpeggioNotes(pattern, scale, rootNote, windNorm, barIndex);
        AddMelodicMovement(pattern, scale, rootNote, precipNorm, barIndex);

        return pattern;
    }

    /// <summary>
    /// Get octave shift based on time of day
    /// Night (0-6, 20-24) = lower, Day (10-16) = higher
    /// </summary>
    private int GetOctaveShiftFromTime(DateTime time)
    {
        int hour = time.Hour;
        
        return hour switch
        {
            >= 0 and < 6 => -1,    // Late night - lowest
            >= 6 and < 10 => 0,    // Morning - mid-low
            >= 10 and < 16 => 1,   // Midday - higher
            >= 16 and < 20 => 0,   // Evening - mid
            _ => -1                 // Night - lowest
        };
    }

    /// <summary>
    /// Add sustained pad notes - higher humidity = longer sustain
    /// </summary>
    private void AddPadNotes(MelodyPattern pattern, ScaleType scale, int rootNote, 
        double humidityNorm, double tempNorm)
    {
        // Base velocity influenced by temperature (warmer = slightly louder)
        int baseVelocity = 50 + (int)(tempNorm * 20);

        // Pad duration based on humidity (more humid = longer sustain)
        long duration = TicksPerHalfNote + (long)(humidityNorm * TicksPerHalfNote);

        // Root note pad
        pattern.Notes.Add(new MelodyNote(
            rootNote,
            0,
            duration,
            baseVelocity,
            MelodyChannel));

        // Add a fifth above for richness (scale degree 4 in most scales ≈ perfect 5th)
        int fifthNote = Scales.GetNote(scale, 4, rootNote);
        pattern.Notes.Add(new MelodyNote(
            fifthNote,
            TicksPerQuarterNote / 2, // Slight offset for depth
            duration - TicksPerQuarterNote / 2,
            baseVelocity - 10,
            MelodyChannel));

        // On high humidity, add third for fuller chord
        if (humidityNorm > 0.6)
        {
            int thirdNote = Scales.GetNote(scale, 2, rootNote);
            pattern.Notes.Add(new MelodyNote(
                thirdNote,
                TicksPerQuarterNote,
                duration - TicksPerQuarterNote,
                baseVelocity - 15,
                MelodyChannel));
        }
    }

    /// <summary>
    /// Add arpeggio notes - wind speed determines density
    /// </summary>
    private void AddArpeggioNotes(MelodyPattern pattern, ScaleType scale, int rootNote,
        double windNorm, int barIndex)
    {
        // Determine note duration based on wind (faster wind = shorter notes)
        long noteDuration;
        int notesPerBar;

        if (windNorm < 0.15)
        {
            // Very calm - no arpeggios, just pads
            return;
        }
        else if (windNorm < 0.3)
        {
            // Light wind - whole notes (1 per bar)
            noteDuration = TicksPerWholeNote - TicksPerQuarterNote;
            notesPerBar = 1;
        }
        else if (windNorm < 0.5)
        {
            // Moderate wind - half notes (2 per bar)
            noteDuration = TicksPerHalfNote - TicksPer8th;
            notesPerBar = 2;
        }
        else if (windNorm < 0.75)
        {
            // Strong wind - quarter notes (4 per bar)
            noteDuration = TicksPerQuarterNote - TicksPer16th;
            notesPerBar = 4;
        }
        else
        {
            // Very strong wind - 8th notes (8 per bar)
            noteDuration = TicksPer8th - TicksPer16th;
            notesPerBar = 8;
        }

        int velocity = 45 + (int)(windNorm * 30);
        long ticksPerNote = TicksPerBar / notesPerBar;

        // Create ascending/descending arpeggio pattern
        bool ascending = barIndex % 2 == 0;
        
        for (int i = 0; i < notesPerBar; i++)
        {
            // Cycle through scale degrees
            int degree = ascending ? (i % 5) + 1 : 5 - (i % 5);
            
            // Add octave variation for interest
            if (i >= notesPerBar / 2)
            {
                degree += 7; // Octave up for second half
            }

            int note = Scales.GetNote(scale, degree, rootNote);
            long position = i * ticksPerNote;

            // Slight velocity variation for natural feel
            int noteVelocity = velocity + _random.Next(-5, 6);

            pattern.Notes.Add(new MelodyNote(
                note,
                position,
                noteDuration,
                Math.Clamp(noteVelocity, 30, 100),
                MelodyChannel));
        }
    }

    /// <summary>
    /// Add melodic movement based on precipitation - rain creates more motion
    /// </summary>
    private void AddMelodicMovement(MelodyPattern pattern, ScaleType scale, int rootNote,
        double precipNorm, int barIndex)
    {
        if (precipNorm < 0.1)
        {
            // No precipitation - no additional movement
            return;
        }

        int velocity = 40 + (int)(precipNorm * 35);
        
        // Higher precipitation = more notes
        int extraNotes = 1 + (int)(precipNorm * 3);

        // Rain creates descending patterns (like falling drops)
        for (int i = 0; i < extraNotes; i++)
        {
            // Descending from higher register
            int degree = 10 - i * 2; // Start high, descend
            int note = Scales.GetNote(scale, degree, rootNote);

            // Scattered timing (raindrops)
            long position = (long)(TicksPerBar * (0.1 + _random.NextDouble() * 0.8));
            long duration = TicksPerQuarterNote + _random.Next(0, TicksPerQuarterNote);

            pattern.Notes.Add(new MelodyNote(
                note,
                position,
                duration,
                velocity + _random.Next(-10, 5),
                MelodyChannel));
        }

        // Heavy rain adds a low rumble note
        if (precipNorm > 0.5)
        {
            int lowNote = rootNote - 12; // Octave below
            pattern.Notes.Add(new MelodyNote(
                Math.Max(24, lowNote), // Don't go too low
                TicksPerHalfNote,
                TicksPerHalfNote,
                velocity - 15,
                MelodyChannel));
        }
    }
}
