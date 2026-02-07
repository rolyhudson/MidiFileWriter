namespace WeatherDrums.Models;

/// <summary>
/// Standard General MIDI drum note numbers (Channel 10)
/// </summary>
public static class DrumNotes
{
    public const int Kick = 36;
    public const int Snare = 38;
    public const int ClosedHiHat = 42;
    public const int OpenHiHat = 46;
    public const int CrashCymbal = 49;
    public const int RideCymbal = 51;
    public const int HighTom = 50;
    public const int MidTom = 47;
    public const int LowTom = 45;
    public const int Clap = 39;
}

/// <summary>
/// Represents a single drum hit event
/// </summary>
public class DrumHit : IMidiNote
{
    /// <summary>
    /// MIDI note number for the drum sound
    /// </summary>
    public int Note { get; set; }

    /// <summary>
    /// Position in ticks from the start of the pattern
    /// </summary>
    public long TickPosition { get; set; }

    /// <summary>
    /// Duration in ticks
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// Velocity (volume) 0-127
    /// </summary>
    public int Velocity { get; set; }

    /// <summary>
    /// MIDI channel - always 9 (channel 10) for drums
    /// </summary>
    public int Channel => 9;

    public DrumHit(int note, long tickPosition, int velocity = 100, long duration = 120)
    {
        Note = note;
        TickPosition = tickPosition;
        Velocity = Math.Clamp(velocity, 0, 127);
        Duration = duration;
    }
}

/// <summary>
/// A collection of drum hits representing a pattern (typically one bar)
/// </summary>
public class DrumPattern : IMidiPattern
{
    public List<DrumHit> Hits { get; set; } = new();
    
    /// <summary>
    /// Length of the pattern in ticks
    /// </summary>
    public long LengthTicks { get; set; }

    /// <summary>
    /// Source weather data that generated this pattern
    /// </summary>
    public HourlyWeatherPoint? SourceWeather { get; set; }

    /// <summary>
    /// Returns all drum hits as IMidiNote
    /// </summary>
    public IEnumerable<IMidiNote> GetNotes() => Hits;
}
