using WeatherDrums.Mapping;
using WeatherDrums.Midi;
using WeatherDrums.Models;
using WeatherDrums.Services;

namespace WeatherDrums;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=================================");
        Console.WriteLine("  Weather MIDI Generator");
        Console.WriteLine("=================================");
        Console.WriteLine();

        // Parse command line arguments
        var config = ParseArguments(args);
        
        if (config.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        try
        {
            // Fetch weather data
            using var weatherService = new WeatherService();
            var weatherData = await weatherService.GetHourlyWeatherAsync(
                config.Latitude, 
                config.Longitude, 
                config.Hours);

            Console.WriteLine();

            // Map weather to drum patterns
            var drumMapper = new WeatherToDrumMapper
            {
                BarsPerHour = config.BarsPerHour
            };
            var drumPatterns = drumMapper.MapWeatherToDrums(weatherData);
            Console.WriteLine($"Generated {drumPatterns.Count} bars of drum patterns");

            // Map weather to melody patterns if enabled
            List<MelodyPattern>? melodyPatterns = null;
            if (config.EnableMelody)
            {
                var melodyMapper = new WeatherToMelodyMapper
                {
                    BarsPerHour = config.BarsPerHour,
                    MelodyChannel = config.MelodyChannel
                };
                melodyPatterns = melodyMapper.MapWeatherToMelody(weatherData);
                Console.WriteLine($"Generated {melodyPatterns.Count} bars of ambient melody");
            }

            Console.WriteLine();

            // Build tracks using the generic MidiTrack API
            var tracks = new List<MidiTrack>
            {
                MidiTrack.CreateDrumTrack("Weather Drums", drumPatterns.Cast<IMidiPattern>())
            };

            if (melodyPatterns != null)
            {
                tracks.Add(MidiTrack.CreateInstrumentTrack(
                    "Weather Melody",
                    config.MelodyChannel,
                    config.MelodyInstrument,
                    melodyPatterns.Cast<IMidiPattern>()));
            }

            // Write MIDI file
            var writer = new MidiFileWriter { Tempo = config.Tempo };
            writer.WriteToFile(config.OutputFile, tracks.ToArray());

            Console.WriteLine();
            Console.WriteLine($"Done! Open '{config.OutputFile}' in your DAW or MIDI player.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static AppConfig ParseArguments(string[] args)
    {
        var config = new AppConfig();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-h":
                case "--help":
                    config.ShowHelp = true;
                    break;

                case "-lat":
                case "--latitude":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out var lat))
                        config.Latitude = lat;
                    break;

                case "-lon":
                case "--longitude":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out var lon))
                        config.Longitude = lon;
                    break;

                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                        config.OutputFile = args[++i];
                    break;

                case "-t":
                case "--tempo":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var tempo))
                        config.Tempo = Math.Clamp(tempo, 40, 300);
                    break;

                case "--hours":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var hours))
                        config.Hours = Math.Clamp(hours, 1, 168); // Max 1 week
                    break;

                case "--bars":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var bars))
                        config.BarsPerHour = Math.Clamp(bars, 1, 16);
                    break;

                case "-m":
                case "--melody":
                    config.EnableMelody = true;
                    break;

                case "--melody-channel":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var channel))
                        config.MelodyChannel = Math.Clamp(channel, 0, 15);
                    break;

                case "--instrument":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var instrument))
                        config.MelodyInstrument = Math.Clamp(instrument, 0, 127);
                    break;
            }
        }

        return config;
    }

    static void PrintHelp()
    {
        Console.WriteLine("Usage: WeatherDrums [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -lat, --latitude <value>    Latitude of location (default: 40.7128 - New York)");
        Console.WriteLine("  -lon, --longitude <value>   Longitude of location (default: -74.0060 - New York)");
        Console.WriteLine("  -o, --output <file>         Output MIDI file path (default: weather_drums.mid)");
        Console.WriteLine("  -t, --tempo <bpm>           Tempo in BPM (default: 120, range: 40-300)");
        Console.WriteLine("  --hours <count>             Hours of forecast to use (default: 24, max: 168)");
        Console.WriteLine("  --bars <count>              Bars per hour of weather (default: 4, range: 1-16)");
        Console.WriteLine("  -m, --melody                Enable ambient melody generation");
        Console.WriteLine("  --melody-channel <0-15>     MIDI channel for melody (default: 0 = channel 1)");
        Console.WriteLine("  --instrument <0-127>        GM instrument for melody (default: 89 = Pad 2 warm)");
        Console.WriteLine("  -h, --help                  Show this help message");
        Console.WriteLine();
        Console.WriteLine("Common GM Instruments for Ambient:");
        Console.WriteLine("  49 = Strings, 89 = Pad 2 (warm), 91 = Pad 4 (choir), 95 = Pad 8 (sweep)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  WeatherDrums");
        Console.WriteLine("  WeatherDrums -lat 51.5074 -lon -0.1278 -o london_drums.mid");
        Console.WriteLine("  WeatherDrums --melody --tempo 80 --hours 48");
        Console.WriteLine("  WeatherDrums -m --melody-channel 1 -o ambient_weather.mid");
        Console.WriteLine();
        Console.WriteLine("Weather to Drum Mapping:");
        Console.WriteLine("  Temperature  -> Kick pattern density (warmer = busier)");
        Console.WriteLine("  Wind Speed   -> Hi-hat pattern (faster wind = 16th notes)");
        Console.WriteLine("  Humidity     -> Snare fills and ghost notes");
        Console.WriteLine("  Precipitation-> Cymbal crashes and ride patterns");
        Console.WriteLine();
        Console.WriteLine("Weather to Melody Mapping (with --melody):");
        Console.WriteLine("  Temperature  -> Scale/mode (cold = minor, warm = major/lydian)");
        Console.WriteLine("  Wind Speed   -> Arpeggio density (calm = pads, windy = fast arpeggios)");
        Console.WriteLine("  Humidity     -> Note sustain (humid = longer, legato notes)");
        Console.WriteLine("  Precipitation-> Melodic movement (rain = descending patterns)");
        Console.WriteLine("  Time of Day  -> Octave register (night = lower, day = higher)");
    }
}

class AppConfig
{
    // Default to New York City coordinates
    public double Latitude { get; set; } = 40.7128;
    public double Longitude { get; set; } = -74.0060;
    public string OutputFile { get; set; } = "weather_drums.mid";
    public int Tempo { get; set; } = 120;
    public int Hours { get; set; } = 24;
    public int BarsPerHour { get; set; } = 4;
    public bool ShowHelp { get; set; } = false;
    public bool EnableMelody { get; set; } = false;
    public int MelodyChannel { get; set; } = 0;
    public int MelodyInstrument { get; set; } = 89; // GM Pad 2 (warm)
}
