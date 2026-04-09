using System.Collections.Generic;
using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// Godot-side runtime hook for <see cref="MusicBeatData"/>. Lives in its own
/// file (excluded from the test compile graph) so the test runner can compile
/// the pure-logic helpers without dragging in Godot.FileAccess, whose static
/// constructor access-violates outside an engine context.
///
/// Wired in <see cref="Autoloads.GameManager._Ready"/> via
/// <c>MusicBeatDataLoader.Install()</c>, which sets the loader hook on
/// MusicBeatData. The hook is invoked the first time anyone calls
/// <see cref="MusicBeatData.EnsureLoaded"/>.
/// </summary>
public static class MusicBeatDataLoader
{
    public const string MainPath      = "res://assets/music/beat_data.json";
    public const string OverridesPath = "res://assets/music/beat_data.overrides.json";

    /// <summary>Wire the runtime loader into the static MusicBeatData hook.</summary>
    public static void Install()
    {
        MusicBeatData.LoaderHook = Populate;
    }

    private static void Populate(Dictionary<string, MusicBeatData.BeatEntry> entries)
    {
        int beforeCount = entries.Count;
        LoadFile(MainPath,      isOverride: false, entries);
        LoadFile(OverridesPath, isOverride: true,  entries);
        GD.Print($"[MusicBeatData] Loaded {entries.Count - beforeCount} beat entries.");
    }

    private static void LoadFile(string resPath, bool isOverride, Dictionary<string, MusicBeatData.BeatEntry> sink)
    {
        if (!Godot.FileAccess.FileExists(resPath)) return;
        using var f = Godot.FileAccess.Open(resPath, Godot.FileAccess.ModeFlags.Read);
        if (f == null) return;
        string text = f.GetAsText();
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            var parsed = MusicBeatData.ParseJson(text, isOverride);
            foreach (var kv in parsed)
                sink[kv.Key] = kv.Value;
        }
        catch (System.Exception ex)
        {
            GD.PushWarning($"[MusicBeatData] Failed to parse {resPath}: {ex.Message}");
        }
    }
}
