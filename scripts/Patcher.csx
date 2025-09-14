#r "../references/Tomlyn/Tomlyn.dll"

#load "Graphics.csx"
#load "Code.csx"
#load "DeepCopy.csx"

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Tomlyn;
using Tomlyn.Helpers;
using Underanalyzer.Decompiler;

EnsureDataLoaded();

var ModsPath = Environment.GetEnvironmentVariable("QLMODSPATH");

var logger = new Logger(Path.Combine(ModsPath, "QuickLoader", "log.txt"));

try {
    foreach (var mod in GetMods(new(ModsPath))) {
        ImportSprites(mod);

        ImportCode(mod);

        if (mod.DataPath is not null) {
            DeepCopyData(mod);
        }

        foreach (var character in mod.Characters) {
            logger.Info(mod.Name, $"Adding character \"{character.Key}\" ...");

            Characters.Add(character);
        }

        if (mod.Patches.Count > 0) {
            logger.Info(mod.Name, "Running patches ...");

            foreach (var patch in mod.Patches) {
                ProcessPatch(mod.Name, patch);
            }
        }
    }

    if (Characters.Count > 0) {
        CreateCharacterPatches();
    }

    if (PatchHistory.Count > 0) {
        logger.Info("QuickLoader", "Updating patched code ...");

        ExecuteAllPatches();
    }

    logger.Info("QuickLoader", "All done :3");

} catch (QuickLoaderException e) {
    logger.Fatal(e.Context, e.Message);
    throw new Exception($"[{e.Context}] {e.Message}", e);
} catch (Exception e) {
    logger.Fatal("QuickLoader", e.Message);
    throw;
}

// custom exception with mod context for better exception logging support and error messages
[Serializable]
public class QuickLoaderException : Exception {
    public string Context { get; }

    public QuickLoaderException() { }

    public QuickLoaderException(string context, string message) : base(message) => Context = context;

    public QuickLoaderException(string context, string message, Exception inner) : base(message, inner) => Context = context;

    protected QuickLoaderException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

public class Logger {
    private enum LogLevel {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    private readonly static int _maxLogLevelLength = Enum.GetNames(typeof(LogLevel))
        .Aggregate((max, current) => max.Length > current.Length ? max : current)
        .Length;

    private readonly string _path;
    private readonly object _lock = new();

    public Logger(string path) => _path = path;

    public void Debug(string context, string message) => Log(LogLevel.Debug, context, message);
    public void Info(string context, string message) => Log(LogLevel.Info, context, message);
    public void Warn(string context, string message) => Log(LogLevel.Warn, context, message);
    public void Error(string context, string message) => Log(LogLevel.Error, context, message);
    public void Fatal(string context, string message) => Log(LogLevel.Fatal, context, message);

    private void Log(LogLevel level, string context, string message) {
        var padding = new string(' ', _maxLogLevelLength - level.ToString().Length);

        lock (_lock) {
            File.AppendAllText(_path, $"[{level.ToString().ToUpper()}]{padding} [{context}] {message}" + Environment.NewLine);
        }
    }
}

public List<Manifest> GetMods(DirectoryInfo modsDirectory) {
    logger.Info("QuickLoader", $"Looking for mods in {modsDirectory.FullName}");

    List<Manifest> mods = [];

    foreach (var modDirectory in modsDirectory.GetDirectories()) {
        var manifestPath = Path.Join(modDirectory.FullName, "manifest.toml");

        if (!File.Exists(manifestPath)) {
            logger.Warn("QuickLoader", $"{modDirectory.Name} does not contain a `manifest.toml`, skipping.");

            continue;
        }

        var manifest = Toml.ToModel<Manifest>(File.ReadAllText(manifestPath), options: Manifest.TomlModelOptions);

        if (!manifest.Validate(out var errors)) {
            logger.Warn("QuickLoader", $"the `manifest.toml` in {modDirectory.Name} failed validation, skipping.");
            logger.Warn("QuickLoader", $"validation errors: {String.Join(", ", errors)}");

            continue;
        }

        if (manifest.Name == "QuickLoader") {
            continue;
        }

        if (File.Exists(Path.Combine(modDirectory.FullName, ".modignore"))) {
            continue;
        }

        logger.Info("QuickLoader", $"Found mod: {manifest.Name}");

        manifest.Directory = modDirectory;

        mods.Add(manifest);
    }

    return mods;
}

internal enum PatchPosition {
    Before,
    At,
    After
}

public class Patch {
    public string Target { get; set; }

    public string Pattern { get; set; }

    public PatchPosition Position { get; set; }

    public string Payload { get; set; }
}

public class Manifest {
    public static readonly PropertyInfo[] _properties = typeof(Manifest).GetProperties();

    public static readonly TomlModelOptions TomlModelOptions = new() {
        ConvertToModel = (obj, type) =>
            obj is string value && type == typeof(Version) ? new Version(value) : null
    };

    [IgnoreDataMember]
    public DirectoryInfo Directory { get; set; }

    public string Name { get; set; }

    public string Author { get; set; }

    public Version Version { get; set; }

    public List<Version> SupportedGameVersions { get; set; }

    public Dictionary<string, string> Characters { get; set; } = [];

    public List<Patch> Patches { get; set; } = [];

    public string? DataPath { get; set; }

    public List<string> GameObjects { get; set; } = [];

    public List<string> Rooms { get; set; } = [];

    public bool Validate(out IEnumerable<string> errors) {
        errors = _properties
            .Select(property => {
                var value = property.GetValue(this);
                
                return property.Name switch {
                    "Name" or "Author" or "Version" or "SupportedGameVersions" when value is null =>
                        $"`{TomlNamingHelper.PascalToSnakeCase(property.Name)}` is required",

                    "SupportedGameVersions" when value is List<Version> list && list.Count == 0 =>
                        "`supported_game_versions` cannot be empty",

                    "DataPath" when (GameObjects.Count > 0 || Rooms.Count > 0) && value is null =>
                        "`data_path` is required if `game_objects` or `rooms` are not empty",

                    _ => null,
                };
            })
            .OfType<string>();

        return !errors.Any();
    }
}
