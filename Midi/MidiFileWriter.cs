using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using WeatherDrums.Mapping;
using WeatherDrums.Models;

namespace WeatherDrums.Midi;

/// <summary>
/// Writes drum and melody patterns to MIDI files using DryWetMIDI
/// </summary>
public class MidiFileWriter
{
    /// <summary>
    /// Tempo in beats per minute
    /// </summary>
    public int Tempo { get; set; } = 120;

    /// <summary>
    /// MIDI program number for melody instrument (General MIDI)
    /// Default: 89 = Pad 2 (warm) - good for ambient
    /// Other options: 49 = Strings, 91 = Pad 4 (choir), 95 = Pad 8 (sweep)
    /// </summary>
    public int MelodyInstrument { get; set; } = 89;

    /// <summary>
    /// MIDI channel for melody (0-15, default 0 = channel 1)
    /// </summary>
    public int MelodyChannel { get; set; } = 0;

    /// <summary>
    /// MIDI channel for drums (standard is channel 10, which is index 9)
    /// </summary>
    private const int DrumChannel = 9;

    /// <summary>
    /// Creates a MIDI file from drum patterns only and saves it to disk
    /// </summary>
    /// <param name="drumPatterns">List of drum patterns to write</param>
    /// <param name="outputPath">Path for the output MIDI file</param>
    public void WriteToFile(List<DrumPattern> drumPatterns, string outputPath)
    {
        WriteToFile(drumPatterns, null, outputPath);
    }

    /// <summary>
    /// Creates a MIDI file from drum and melody patterns and saves it to disk
    /// </summary>
    /// <param name="drumPatterns">List of drum patterns to write</param>
    /// <param name="melodyPatterns">Optional list of melody patterns to write</param>
    /// <param name="outputPath">Path for the output MIDI file</param>
    public void WriteToFile(List<DrumPattern> drumPatterns, List<MelodyPattern>? melodyPatterns, string outputPath)
    {
        Console.WriteLine($"Creating MIDI file at {outputPath}...");
        Console.WriteLine($"Tempo: {Tempo} BPM");
        Console.WriteLine($"Drum patterns: {drumPatterns.Count} bars");
        if (melodyPatterns != null)
        {
            Console.WriteLine($"Melody patterns: {melodyPatterns.Count} bars");
        }

        // Set the time division (ticks per quarter note)
        var timeDivision = new TicksPerQuarterNoteTimeDivision((short)WeatherToDrumMapper.TicksPerQuarterNote);
        var midiFile = new MidiFile { TimeDivision = timeDivision };
        
        // Create drum track
        var drumTrack = new TrackChunk();
        drumTrack.Events.Add(new SequenceTrackNameEvent("Weather Drums"));
        
        // Add tempo event to drum track (microseconds per quarter note)
        var microsecondsPerBeat = 60_000_000 / Tempo;
        drumTrack.Events.Add(new SetTempoEvent(microsecondsPerBeat));
        
        midiFile.Chunks.Add(drumTrack);

        // Add drum notes
        var drumNoteCount = AddDrumNotesToTrack(drumTrack, drumPatterns);

        // Create melody track if patterns provided
        int melodyNoteCount = 0;
        if (melodyPatterns != null && melodyPatterns.Count > 0)
        {
            var melodyTrack = new TrackChunk();
            melodyTrack.Events.Add(new SequenceTrackNameEvent("Weather Melody"));
            
            // Set the instrument for the melody channel (Program Change event)
            // This tells the MIDI player what sound to use
            melodyTrack.Events.Add(new ProgramChangeEvent((SevenBitNumber)MelodyInstrument)
            {
                Channel = (FourBitNumber)MelodyChannel
            });
            
            midiFile.Chunks.Add(melodyTrack);

            melodyNoteCount = AddMelodyNotesToTrack(melodyTrack, melodyPatterns);
            
            Console.WriteLine($"Melody instrument: {MelodyInstrument} (GM Pad) on channel {MelodyChannel + 1}");
        }

        // Write to file
        midiFile.Write(outputPath, overwriteFile: true);

        // Calculate duration
        var totalTicks = drumPatterns.Sum(p => p.LengthTicks);
        var totalBeats = totalTicks / WeatherToDrumMapper.TicksPerQuarterNote;
        var durationSeconds = totalBeats * 60.0 / Tempo;
        var duration = TimeSpan.FromSeconds(durationSeconds);

        Console.WriteLine($"MIDI file created successfully!");
        Console.WriteLine($"Duration: {duration:mm\\:ss}");
        Console.WriteLine($"Drum notes: {drumNoteCount}");
        if (melodyPatterns != null)
        {
            Console.WriteLine($"Melody notes: {melodyNoteCount}");
            Console.WriteLine($"Total notes: {drumNoteCount + melodyNoteCount}");
        }
    }

    private int AddDrumNotesToTrack(TrackChunk trackChunk, List<DrumPattern> patterns)
    {
        using var notesManager = trackChunk.ManageNotes();
        
        int noteCount = 0;
        long currentPosition = 0;

        foreach (var pattern in patterns)
        {
            foreach (var hit in pattern.Hits)
            {
                var note = new Note(
                    (SevenBitNumber)hit.Note,
                    hit.Duration,
                    currentPosition + hit.TickPosition)
                {
                    Velocity = (SevenBitNumber)hit.Velocity,
                    Channel = (FourBitNumber)DrumChannel
                };
                
                notesManager.Objects.Add(note);
                noteCount++;
            }

            currentPosition += pattern.LengthTicks;
        }

        return noteCount;
    }

    private int AddMelodyNotesToTrack(TrackChunk trackChunk, List<MelodyPattern> patterns)
    {
        using var notesManager = trackChunk.ManageNotes();
        
        int noteCount = 0;
        long currentPosition = 0;

        foreach (var pattern in patterns)
        {
            foreach (var melodyNote in pattern.Notes)
            {
                var note = new Note(
                    (SevenBitNumber)melodyNote.Note,
                    melodyNote.Duration,
                    currentPosition + melodyNote.TickPosition)
                {
                    Velocity = (SevenBitNumber)melodyNote.Velocity,
                    Channel = (FourBitNumber)melodyNote.Channel
                };
                
                notesManager.Objects.Add(note);
                noteCount++;
            }

            currentPosition += pattern.LengthTicks;
        }

        return noteCount;
    }
}
