using System.Collections.Generic;
using System.Text.Json;

namespace SennenRpg.Core.Data;

/// <summary>
/// Storage + pure-logic helpers for the auto-detected BGM beat data. The
/// actual disk-loading code lives in <c>MusicBeatDataLoader.cs</c> (main
/// project only) so this file can be compiled into the test runner without
/// dragging in <see cref="Godot.FileAccess"/>, whose static constructor
/// access-violates outside an engine context.
///
/// Files loaded at runtime:
///   <c>res://assets/music/beat_data.json</c>           — auto-detected
///   <c>res://assets/music/beat_data.overrides.json</c> — hand-corrected
/// Override entries always win.
/// </summary>
public static class MusicBeatData
{
    public sealed record BeatEntry(float Bpm, float FirstBeatSec, float Confidence, bool Override);

    private static readonly Dictionary<string, BeatEntry> _entries = new();
    private static bool _loaded;

    /// <summary>
    /// Idempotent — call once at startup. The Godot-side runtime loader is
    /// hooked in via <see cref="LoaderHook"/>; tests leave it unset and this
    /// becomes a no-op (entries stay empty, callers fall back to hardcoded
    /// MusicMetadata values).
    /// </summary>
    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        LoaderHook?.Invoke(_entries);
    }

    /// <summary>
    /// Set by the runtime loader (see <c>MusicBeatDataLoader</c>) at autoload
    /// time. The hook receives the live entry dictionary and is expected to
    /// populate it from the JSON sidecars.
    /// </summary>
    public static System.Action<Dictionary<string, BeatEntry>>? LoaderHook { get; set; }

    public static bool TryGet(string resourcePath, out BeatEntry entry)
    {
        EnsureLoaded();
        return _entries.TryGetValue(resourcePath, out entry!);
    }

    /// <summary>Total entries currently loaded — used for debugging / tests.</summary>
    public static int Count => _entries.Count;

    /// <summary>Reset state — test-only escape hatch.</summary>
    internal static void ResetForTests()
    {
        _entries.Clear();
        _loaded = false;
        LoaderHook = null;
    }

    /// <summary>Inject an entry directly — test-only escape hatch.</summary>
    internal static void InjectForTests(string path, BeatEntry entry)
    {
        _entries[path] = entry;
        _loaded = true;
    }

    /// <summary>
    /// Pure-logic JSON parser. Used by both the runtime loader and tests.
    /// </summary>
    public static Dictionary<string, BeatEntry> ParseJson(string json, bool isOverride)
    {
        var result = new Dictionary<string, BeatEntry>();
        if (string.IsNullOrEmpty(json)) return result;
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var obj = prop.Value;
            float bpm  = obj.GetProperty("bpm").GetSingle();
            float off  = obj.GetProperty("first_beat_sec").GetSingle();
            float conf = obj.TryGetProperty("confidence", out var c) ? c.GetSingle() : 1f;
            result[prop.Name] = new BeatEntry(bpm, off, conf, isOverride);
        }
        return result;
    }

    /// <summary>
    /// Pure-logic layering used by both the runtime loader and tests.
    /// Override entries replace base entries.
    /// </summary>
    public static Dictionary<string, BeatEntry> Layer(
        IDictionary<string, BeatEntry> baseEntries,
        IDictionary<string, BeatEntry> overrides)
    {
        var result = new Dictionary<string, BeatEntry>(baseEntries);
        foreach (var kv in overrides)
            result[kv.Key] = kv.Value;
        return result;
    }
}
