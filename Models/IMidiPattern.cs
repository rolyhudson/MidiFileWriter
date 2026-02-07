namespace WeatherDrums.Models;

/// <summary>
/// Common interface for all MIDI note types (drum hits, melody notes, etc.)
/// </summary>
public interface IMidiNote
{
    /// <summary>
    /// MIDI note number (0-127)
    /// </summary>
    int Note { get; }

    /// <summary>
    /// Position in ticks from the start of the pattern
    /// </summary>
    long TickPosition { get; }

    /// <summary>
    /// Duration in ticks
    /// </summary>
    long Duration { get; }

    /// <summary>
    /// Velocity (volume) 0-127
    /// </summary>
    int Velocity { get; }

    /// <summary>
    /// MIDI channel (0-15)
    /// </summary>
    int Channel { get; }
}

/// <summary>
/// Common interface for all pattern types (drum patterns, melody patterns, etc.)
/// </summary>
public interface IMidiPattern
{
    /// <summary>
    /// Length of the pattern in ticks
    /// </summary>
    long LengthTicks { get; }

    /// <summary>
    /// Returns all notes in this pattern as IMidiNote
    /// </summary>
    IEnumerable<IMidiNote> GetNotes();
}

/// <summary>
/// Represents a MIDI track with metadata and patterns
/// </summary>
public class MidiTrack
{
    /// <summary>
    /// Display name for the track
    /// </summary>
    public string Name { get; set; } = "Track";

    /// <summary>
    /// MIDI channel (0-15). Channel 9 is reserved for drums.
    /// </summary>
    public int Channel { get; set; } = 0;

    /// <summary>
    /// GM instrument number (0-127). Null for drums (channel 9).
    /// </summary>
    public int? Instrument { get; set; }

    /// <summary>
    /// The patterns to write to this track
    /// </summary>
    public List<IMidiPattern> Patterns { get; set; } = new();

    /// <summary>
    /// Creates a drum track (channel 9, no instrument)
    /// </summary>
    public static MidiTrack CreateDrumTrack(string name, IEnumerable<IMidiPattern> patterns)
    {
        return new MidiTrack
        {
            Name = name,
            Channel = 9,
            Instrument = null,
            Patterns = patterns.ToList()
        };
    }

    /// <summary>
    /// Creates a melody/instrument track with specified channel and instrument
    /// </summary>
    public static MidiTrack CreateInstrumentTrack(string name, int channel, int instrument, IEnumerable<IMidiPattern> patterns)
    {
        return new MidiTrack
        {
            Name = name,
            Channel = channel,
            Instrument = instrument,
            Patterns = patterns.ToList()
        };
    }
}
