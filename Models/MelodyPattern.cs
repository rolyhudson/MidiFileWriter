namespace WeatherDrums.Models;

/// <summary>
/// Musical scales for ambient melody generation
/// </summary>
public enum ScaleType
{
    Minor,      // Natural minor / Aeolian - dark, cold
    Dorian,     // Minor with raised 6th - melancholic
    Pentatonic, // 5-note scale - neutral, floating
    Lydian,     // Major with raised 4th - dreamy, warm
    Major,      // Ionian - bright, open
    WholeTone   // All whole steps - ethereal, ambiguous
}

/// <summary>
/// Helper class for scale note generation
/// </summary>
public static class Scales
{
    // Root note (C4 = MIDI note 60)
    public const int DefaultRoot = 60;

    // Scale intervals from root (in semitones)
    private static readonly Dictionary<ScaleType, int[]> ScaleIntervals = new()
    {
        [ScaleType.Minor] = [0, 2, 3, 5, 7, 8, 10],        // W-H-W-W-H-W-W
        [ScaleType.Dorian] = [0, 2, 3, 5, 7, 9, 10],       // W-H-W-W-W-H-W
        [ScaleType.Pentatonic] = [0, 2, 4, 7, 9],          // Minor pentatonic
        [ScaleType.Lydian] = [0, 2, 4, 6, 7, 9, 11],       // W-W-W-H-W-W-H
        [ScaleType.Major] = [0, 2, 4, 5, 7, 9, 11],        // W-W-H-W-W-W-H
        [ScaleType.WholeTone] = [0, 2, 4, 6, 8, 10]        // W-W-W-W-W-W
    };

    /// <summary>
    /// Get MIDI note number for a scale degree
    /// </summary>
    /// <param name="scale">The scale type</param>
    /// <param name="degree">Scale degree (0 = root, can be negative or > scale length for octave shifts)</param>
    /// <param name="root">Root MIDI note (default C4 = 60)</param>
    public static int GetNote(ScaleType scale, int degree, int root = DefaultRoot)
    {
        var intervals = ScaleIntervals[scale];
        var scaleLength = intervals.Length;

        // Handle octave wrapping
        int octaveShift = degree >= 0 
            ? degree / scaleLength 
            : (degree - scaleLength + 1) / scaleLength;
        
        int normalizedDegree = ((degree % scaleLength) + scaleLength) % scaleLength;

        return root + (octaveShift * 12) + intervals[normalizedDegree];
    }

    /// <summary>
    /// Get all notes in a scale within a given octave range
    /// </summary>
    public static List<int> GetScaleNotes(ScaleType scale, int root = DefaultRoot, int octaves = 2)
    {
        var notes = new List<int>();
        var intervals = ScaleIntervals[scale];

        for (int octave = 0; octave < octaves; octave++)
        {
            foreach (var interval in intervals)
            {
                var note = root + (octave * 12) + interval;
                if (note <= 127) notes.Add(note);
            }
        }

        return notes;
    }

    /// <summary>
    /// Select scale based on temperature (cold = minor, warm = major)
    /// </summary>
    public static ScaleType SelectScaleByTemperature(double temperatureCelsius)
    {
        return temperatureCelsius switch
        {
            < 0 => ScaleType.Minor,
            < 10 => ScaleType.Dorian,
            < 20 => ScaleType.Pentatonic,
            < 30 => ScaleType.Lydian,
            _ => ScaleType.Major
        };
    }
}

/// <summary>
/// Represents a single melodic note event
/// </summary>
public class MelodyNote
{
    /// <summary>
    /// MIDI note number (0-127)
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
    /// MIDI channel (0-15, default 0 for channel 1)
    /// </summary>
    public int Channel { get; set; }

    public MelodyNote(int note, long tickPosition, long duration, int velocity = 80, int channel = 0)
    {
        Note = Math.Clamp(note, 0, 127);
        TickPosition = tickPosition;
        Duration = duration;
        Velocity = Math.Clamp(velocity, 0, 127);
        Channel = Math.Clamp(channel, 0, 15);
    }
}

/// <summary>
/// A collection of melody notes representing a pattern (typically one bar)
/// </summary>
public class MelodyPattern
{
    public List<MelodyNote> Notes { get; set; } = new();

    /// <summary>
    /// Length of the pattern in ticks
    /// </summary>
    public long LengthTicks { get; set; }

    /// <summary>
    /// The scale used for this pattern
    /// </summary>
    public ScaleType Scale { get; set; }

    /// <summary>
    /// Root note for the pattern
    /// </summary>
    public int RootNote { get; set; } = Scales.DefaultRoot;

    /// <summary>
    /// Source weather data that generated this pattern
    /// </summary>
    public HourlyWeatherPoint? SourceWeather { get; set; }
}
