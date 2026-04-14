using System.Collections.Generic;
using System.Globalization;

namespace SennenRpg.Core.Data;

/// <summary>
/// Static lookup table mapping <c>MapId</c> values (or full <c>.tscn</c> paths)
/// to human-readable display names shown on save slot cards and other UI.
/// Unknown ids fall back to a prettified title-case of the raw id.
/// </summary>
public static class MapDisplayNames
{
    private static readonly Dictionary<string, string> ById = new(System.StringComparer.OrdinalIgnoreCase)
    {
        ["mapp_tavern"]     = "Mapp Tavern",
        ["mellyr_outpost"]  = "Mellyr Outpost",
        ["world_map"]       = "World Map",
        ["dungeon_floor1"]  = "Dungeon - Floor 1",
        ["dungeon_floor2"]  = "Dungeon - Floor 2",
        ["dungeon_floor3"]  = "Dungeon - Floor 3",
        // MapIds baked directly into some dungeon .tscn files
        ["Dungeon - Floor 1"] = "Dungeon - Floor 1",
        ["Dungeon - Floor 2"] = "Dungeon - Floor 2",
        ["Dungeon - Floor 3"] = "Dungeon - Floor 3",
    };

    /// <summary>
    /// Returns the display name for a given map id. Unknown ids are prettified
    /// (underscores → spaces, title-cased). Empty/null → "Unknown".
    /// </summary>
    public static string ForId(string? mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId)) return "Unknown";
        if (ById.TryGetValue(mapId, out var name)) return name;
        return Prettify(mapId!);
    }

    /// <summary>
    /// Returns the display name for a map given its scene path
    /// (e.g. <c>res://scenes/overworld/MAPP.tscn</c>). Falls back to prettifying
    /// the file stem when the path is unknown. Empty/null → "Unknown".
    /// </summary>
    public static string ForPath(string? scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath)) return "Unknown";

        // Extract the file stem (e.g. "MAPP" from "res://scenes/overworld/MAPP.tscn")
        int lastSlash = scenePath!.LastIndexOfAny(new[] { '/', '\\' });
        string file   = lastSlash >= 0 ? scenePath.Substring(lastSlash + 1) : scenePath;
        int dot       = file.LastIndexOf('.');
        string stem   = dot >= 0 ? file.Substring(0, dot) : file;

        // Known path-stem aliases
        switch (stem)
        {
            case "MAPP":           return "Mapp Tavern";
            case "WorldMap":       return "World Map";
            case "MellyrOutpost":  return "Mellyr Outpost";
            case "DungeonFloor1":  return "Dungeon - Floor 1";
            case "DungeonFloor2":  return "Dungeon - Floor 2";
            case "DungeonFloor3":  return "Dungeon - Floor 3";
        }

        if (ById.TryGetValue(stem, out var named)) return named;
        return Prettify(stem);
    }

    /// <summary>Converts "test_room" / "testRoom" → "Test Room".</summary>
    private static string Prettify(string raw)
    {
        string spaced = raw.Replace('_', ' ').Replace('-', ' ').Trim();
        if (spaced.Length == 0) return "Unknown";
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.ToLowerInvariant());
    }
}
