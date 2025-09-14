using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;
using Underanalyzer.Decompiler;

static readonly DecompileSettings decompileSettings_ = new() {
    RemoveSingleLineBlockBraces = true,
    EmptyLineAroundBranchStatements = true,
    EmptyLineBeforeSwitchCases = true
};

static readonly List<string> CodeExtensions = [".gml", ".asm"];

static readonly Regex NewlinePattern = new(@"\r\n|\n", RegexOptions.Compiled);

static readonly Regex TrailingNewlinePattern = new(@"\r\n|\n$", RegexOptions.Compiled);

static readonly Func<string, HashSet<string>, string> AlreadyPatchedMessage =
    (name, mods) => $"The code entry \"{name}\" has already been patched, you may be attempting to overwrite another mod's changes, try checking the patches in the following mod(s): ({String.Join(", ", mods)})";

public Dictionary<string, (string Format, List<string> Lines, HashSet<string> Mods)> PatchHistory = [];

public List<KeyValuePair<string, string>> Characters = [];

public void ImportCode(Manifest mod) {
    var groups = mod.Directory.GetFiles("*.*", SearchOption.AllDirectories)
        .Where(file => CodeExtensions.Contains(file.Extension))
        .GroupBy(file => file.Extension);

    if (groups.Count() == 0) {
        return;
    }

    if (groups.SingleOrDefault(group => group.Key == ".gml") is IGrouping<string, FileInfo> scripts) {
        var count = scripts.Count();
        logger.Info(mod.Name, $"Importing {count} {(count > 1 ? "scripts" : "script")} ...");

        var importGroup = new CodeImportGroup(Data);

        foreach (var script in scripts) {
            importGroup.QueueReplace(
                Path.GetFileNameWithoutExtension(script.Name),
                File.ReadAllText(script.FullName)
            );
        }

        importGroup.Import();
    }

    if (groups.SingleOrDefault(group => group.Key == ".asm") is Grouping<string, FileInfo> assemblies) {
        var count = assemblies.Count();

        logger.Info(mod.Name, $"Importing {count} {(count > 1 ? "assemblies" : "assembly")} ...");

        foreach (var assembly in assemblies) {
            Data.Code.ByName(Path.GetFileNameWithoutExtension(assembly.Name))
                .Replace(Assembler.Assemble(File.ReadAllText(assembly.FullName), Data));
        }
    }
}

public void CreateCharacterPatches() {
    var names = Characters.Select(pair => $"\"{pair.Key}\"");

    ProcessPatch("QuickLoader", new() {
        Target = "gml_Object_obj_css_portraits_list_Create_0.gml",
        Pattern = "validCharacters = [\"eureka\", \"knockt\", \"rend\", \"ninja\"];",
        Position = PatchPosition.At,
        Payload = $"validCharacters = [\"eureka\", \"knockt\", \"rend\", \"ninja\", {String.Join(", ", names)}];"
    });

    ProcessPatch("QuickLoader", new() {
        Target = "gml_GlobalScript_character_data.gml",
        Pattern = "static _data",
        Position = PatchPosition.At,
        Payload = $"""
static _data = [
    character_define("eureka", anim_timings_eureka, character_init_eureka, spr_css_portrait_eureka, spr_css_idle_eureka, spr_css_selected_start_eureka, spr_css_selected_loop_eureka, snd_css_eureka, spr_win_start_eureka, spr_win_loop_eureka, spr_lose_loop_eureka, spr_win_eureka, snd_win_eureka, spr_palette_eureka, spr_hud_eureka, spr_stock_eureka, ["texture_gameplay_eureka"], cpu_script_eureka),
    character_define("knockt", anim_timings_knockt, character_init_knockt, spr_css_portrait_knockt, spr_css_idle_knockt, spr_css_selected_start_knockt, spr_css_selected_loop_knockt, snd_css_knockt, spr_win_start_knockt, spr_win_loop_knockt, spr_lose_loop_knockt, spr_win_knockt, snd_win_knockt, spr_palette_knockt, spr_hud_knockt, spr_stock_knockt, ["texture_gameplay_knockt"], cpu_script_knockt),
    character_define("rend", anim_timings_rend, character_init_rend, spr_css_portrait_rend, spr_css_idle_rend, spr_css_selected_start_rend, spr_css_selected_loop_rend, snd_css_rend, spr_win_start_rend, spr_win_loop_rend, spr_lose_loop_rend, spr_win_rend, snd_win_rend, spr_palette_rend, spr_hud_rend, spr_stock_rend, ["texture_gameplay_rend"], cpu_script_rend),
    character_define("ninja", anim_timings_ninja, character_init_ninja, spr_css_portrait_ninja, spr_css_idle_ninja, spr_css_selected_start_ninja, spr_css_selected_loop_ninja, snd_css_ninja, spr_win_start_ninja, spr_win_loop_ninja, spr_lose_loop_ninja, spr_win_ninja, snd_win_ninja, spr_palette_ninja, spr_hud_ninja, spr_stock_ninja, ["texture_gameplay_ninja"], cpu_script_ninja),
    character_define("Random", anim_timings_rend, character_init_rend, spr_css_portrait_rend, spr_css_idle_rend, spr_css_selected_start_rend, spr_css_selected_loop_rend, snd_activation, spr_win_start_rend, spr_win_loop_rend, spr_lose_loop_rend, spr_win_rend, snd_win_rend, spr_palette_rend, spr_hud_rend, spr_stock_rend, ["texture_gameplay_rend"], cpu_script_rend),
    {String.Join(",\n", Characters.Select(pair => pair.Value))}
];
"""
    });
}

public void ProcessPatch(string modName, Patch patch) {
    var name = Path.GetFileNameWithoutExtension(patch.Target);
    var extension = Path.GetExtension(patch.Target);

    // try to get the history page for this code entry if it exists, and create it if it doesn't
    if (!PatchHistory.TryGetValue(name, out var page)) {
        if (!(Data.Code.ByName(name) is UndertaleCode code)) {
            throw new QuickLoaderException(modName, $"No code entry was found with the name \"{name}\".");
        }

        // get the source code for either of the two supported formats
        var source = extension switch {
            ".gml" => GetDecompiledText(code, settings: decompileSettings_),
            ".asm" => GetDisassemblyText(code),

            _ => throw new QuickLoaderException(modName, $"\"{extension}\" is not a supported code format."),
        };

        page = (extension, source.Split('\n').ToList(), []);
        PatchHistory.Add(name, page);
    } else if (extension != page.Format) {
        throw new QuickLoaderException(modName, $"The code entry \"{name}\" already has patches that target its \"{page.Format}\" format, only one format can be patched per code entry.");
    }

    var lineNumbers = Enumerable.Range(0, page.Lines.Count);

    // remove one trailing newline as a result of multiline strings toml
    if (TrailingNewlinePattern.Match(patch.Pattern) is Match patternMatch && patternMatch.Success) {
        patch.Pattern = patch.Pattern.Remove(patch.Pattern.Length - patternMatch.Value.Length);
    }

    if (TrailingNewlinePattern.Match(patch.Payload) is Match payloadMatch && payloadMatch.Success) {
        patch.Payload = patch.Payload.Remove(patch.Payload.Length - payloadMatch.Value.Length);
    }

    // patching logic for single line search patterns
    if (!NewlinePattern.IsMatch(patch.Pattern)) {
        var matchedLineNumbers = lineNumbers.Where(lineNumber => page.Lines[lineNumber].Contains(patch.Pattern)).ToList();

        // ensure there is only one occurrence of the search pattern in the source code
        if (matchedLineNumbers.Count != 1) {
            if (page.Mods.Count > 0) {
                logger.Error(modName, AlreadyPatchedMessage(name, page.Mods));
            }

            if (matchedLineNumbers.Count == 0) {
                throw new QuickLoaderException(modName, $"Unable to find a match for the following pattern{(page.Mods.Count > 0 ? "(check the log for more details)" : "")} : {patch.Pattern}");
            } else if (matchedLineNumbers.Count > 1) {
                throw new QuickLoaderException(modName, $"Too many matches were found for the following pattern{(page.Mods.Count > 0 ? "(check the log for more details)" : "")} : {patch.Pattern}");
            }
        }

        var matchedLineNumber = matchedLineNumbers[0];
        var indentation = new string(' ', page.Lines[matchedLineNumber].TakeWhile(Char.IsWhiteSpace).Count());
        var payloadLines = NewlinePattern.Split(patch.Payload).Select(line => indentation + line);

        switch (patch.Position) {
            case PatchPosition.At:
                page.Lines.RemoveAt(matchedLineNumber);
                if (!String.IsNullOrWhiteSpace(patch.Payload)) {
                    page.Lines.InsertRange(matchedLineNumber, payloadLines);
                }
                break;

            case PatchPosition.Before:
                page.Lines.InsertRange(matchedLineNumber, payloadLines);
                break;

            case PatchPosition.After:
                page.Lines.InsertRange(matchedLineNumber + 1, payloadLines);
                break;
        }
    // patching logic for multiline search patterns
    } else {
        var trimmedPatternLines = NewlinePattern.Split(patch.Pattern).Select(line => line.Trim()).ToList();
        var firstTrimmedPatternLine = trimmedPatternLines.First();
        var matchedFirstLineNumbers = lineNumbers.Where(lineNumber => page.Lines[lineNumber].Trim() == firstTrimmedPatternLine);

        List<int> matchedLineNumbers = [];
        foreach (var matchedFirstLineNumber in matchedFirstLineNumbers) {
            matchedLineNumbers = Enumerable.Range(matchedFirstLineNumber + 1, trimmedPatternLines.Count - 1)
                .Where((lineNumber, index) => page.Lines[lineNumber].Trim() == trimmedPatternLines[index + 1])
                .Prepend(matchedFirstLineNumber)
                .ToList();

            if (matchedLineNumbers.Count == trimmedPatternLines.Count) {
                break;
            }
        }

        if (matchedLineNumbers.Count != trimmedPatternLines.Count) {
            if (page.Mods.Count > 0) {
                logger.Error(modName, AlreadyPatchedMessage(name, page.Mods));
            }
            throw new QuickLoaderException(modName, $"Unable to find a full match for the following multiline pattern{(page.Mods.Count > 1 ? "(check the log for more details)" : "")} : {patch.Pattern}");
        }

        var indentation = new string(' ', matchedLineNumbers
            .Where(lineNumber => !String.IsNullOrWhiteSpace(page.Lines[lineNumber]))
            .Select(lineNumber => page.Lines[lineNumber].TakeWhile(Char.IsWhiteSpace).Count())
            .Min());

        var payloadLines = NewlinePattern.Split(patch.Payload).Select(line => indentation + line);

        switch (patch.Position) {
            case PatchPosition.At:
                page.Lines.RemoveRange(matchedLineNumbers.First(), trimmedPatternLines.Count);
                if (!String.IsNullOrWhiteSpace(patch.Payload)) {
                    page.Lines.InsertRange(matchedLineNumbers.First(), payloadLines);
                }
                break;

            case PatchPosition.Before:
                page.Lines.InsertRange(matchedLineNumbers.First(), payloadLines);
                break;

            case PatchPosition.After:
                page.Lines.InsertRange(matchedLineNumbers.Last() + 1, payloadLines);
                break;
        }
    }

    PatchHistory[name].Mods.Add(modName);
}

public void ExecuteAllPatches() {
    var importGroup = new CodeImportGroup(Data);

    foreach (var (name, (format, lines, _)) in PatchHistory) {
        if (format == ".gml") {
            logger.Debug("QuickLoader", $"Updating gml of \"{name}\".");
            importGroup.QueueReplace(name, String.Join('\n', lines));
        }

        if (format == ".asm") {
            logger.Debug("QuickLoader", $"Updating assembly of \"{name}\".");
            Data.Code.ByName(name).Replace(Assembler.Assemble(String.Join('\n', lines), Data));
        }
    }

    importGroup.Import();
}
