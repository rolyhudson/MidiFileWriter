using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using WeatherDrums.Mapping;
using WeatherDrums.Models;

namespace WeatherDrums.Midi;

/// <summary>
/// Writes MIDI patterns to files using DryWetMIDI.
/// Supports any pattern type implementing IMidiPattern.
/// </summary>
public class MidiFileWriter
{
    /// <summary>
    /// Tempo in beats per minute
    /// </summary>
    public int Tempo { get; set; } = 120;

    /// <summary>
    /// Ticks per quarter note (standard MIDI resolution)
    /// </summary>
    public int TicksPerQuarterNote { get; set; } = WeatherToDrumMapper.TicksPerQuarterNote;

    /// <summary>
    /// Creates a MIDI file from one or more tracks and saves it to disk
    /// </summary>
    /// <param name="outputPath">Path for the output MIDI file</param>
    /// <param name="tracks">One or more MidiTrack objects to write</param>
    public void WriteToFile(string outputPath, params MidiTrack[] tracks)
    {
        if (tracks.Length == 0)
        {
            throw new ArgumentException("At least one track is required", nameof(tracks));
        }

        Console.WriteLine($"Creating MIDI file at {outputPath}...");
        Console.WriteLine($"Tempo: {Tempo} BPM");
        Console.WriteLine($"Tracks: {tracks.Length}");

        // Set the time division (ticks per quarter note)
        var timeDivision = new TicksPerQuarterNoteTimeDivision((short)TicksPerQuarterNote);
        var midiFile = new MidiFile { TimeDivision = timeDivision };

        int totalNotes = 0;
        long maxTicks = 0;
        bool tempoSet = false;

        foreach (var track in tracks)
        {
            var trackChunk = new TrackChunk();
            
            // Add track name
            trackChunk.Events.Add(new SequenceTrackNameEvent(track.Name));

            // Add tempo event to the first track only
            if (!tempoSet)
            {
                var microsecondsPerBeat = 60_000_000 / Tempo;
                trackChunk.Events.Add(new SetTempoEvent(microsecondsPerBeat));
                tempoSet = true;
            }

            // Add program change for non-drum tracks
            if (track.Instrument.HasValue && track.Channel != 9)
            {
                trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)track.Instrument.Value)
                {
                    Channel = (FourBitNumber)track.Channel
                });
                Console.WriteLine($"  {track.Name}: Channel {track.Channel + 1}, Instrument {track.Instrument.Value}");
            }
            else
            {
                Console.WriteLine($"  {track.Name}: Channel {track.Channel + 1} (drums)");
            }

            midiFile.Chunks.Add(trackChunk);

            // Add notes from all patterns
            var (noteCount, trackTicks) = AddNotesToTrack(trackChunk, track);
            totalNotes += noteCount;
            maxTicks = Math.Max(maxTicks, trackTicks);

            Console.WriteLine($"    Patterns: {track.Patterns.Count}, Notes: {noteCount}");
        }

        // Write to file
        midiFile.Write(outputPath, overwriteFile: true);

        // Calculate duration
        var totalBeats = maxTicks / TicksPerQuarterNote;
        var durationSeconds = totalBeats * 60.0 / Tempo;
        var duration = TimeSpan.FromSeconds(durationSeconds);

        Console.WriteLine($"MIDI file created successfully!");
        Console.WriteLine($"Duration: {duration:mm\\:ss}");
        Console.WriteLine($"Total notes: {totalNotes}");
    }

    /// <summary>
    /// Backward compatible: Creates a MIDI file from drum patterns only
    /// </summary>
    public void WriteToFile(List<DrumPattern> drumPatterns, string outputPath)
    {
        var drumTrack = MidiTrack.CreateDrumTrack("Weather Drums", drumPatterns.Cast<IMidiPattern>());
        WriteToFile(outputPath, drumTrack);
    }

    /// <summary>
    /// Backward compatible: Creates a MIDI file from drum and melody patterns
    /// </summary>
    public void WriteToFile(List<DrumPattern> drumPatterns, List<MelodyPattern>? melodyPatterns, 
        string outputPath, int melodyChannel = 0, int melodyInstrument = 89)
    {
        var tracks = new List<MidiTrack>
        {
            MidiTrack.CreateDrumTrack("Weather Drums", drumPatterns.Cast<IMidiPattern>())
        };

        if (melodyPatterns != null && melodyPatterns.Count > 0)
        {
            tracks.Add(MidiTrack.CreateInstrumentTrack(
                "Weather Melody", 
                melodyChannel, 
                melodyInstrument, 
                melodyPatterns.Cast<IMidiPattern>()));
        }

        WriteToFile(outputPath, tracks.ToArray());
    }

    /// <summary>
    /// Adds notes from a track's patterns to a MIDI track chunk
    /// </summary>
    private (int noteCount, long totalTicks) AddNotesToTrack(TrackChunk trackChunk, MidiTrack track)
    {
        using var notesManager = trackChunk.ManageNotes();
        
        int noteCount = 0;
        long currentPosition = 0;

        foreach (var pattern in track.Patterns)
        {
            foreach (var midiNote in pattern.GetNotes())
            {
                var note = new Note(
                    (SevenBitNumber)midiNote.Note,
                    midiNote.Duration,
                    currentPosition + midiNote.TickPosition)
                {
                    Velocity = (SevenBitNumber)midiNote.Velocity,
                    Channel = (FourBitNumber)midiNote.Channel
                };
                
                notesManager.Objects.Add(note);
                noteCount++;
            }

            currentPosition += pattern.LengthTicks;
        }

        return (noteCount, currentPosition);
    }
}
