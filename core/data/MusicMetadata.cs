using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Static registry mapping music resource paths to track metadata.
/// All metadata is defined here rather than parsed from filenames.
/// </summary>
public static class MusicMetadata
{
    private static readonly Dictionary<string, MusicTrackInfo> Registry = new()
    {
        ["res://assets/music/Carillion Forest.wav"] = new(
            "Divora", "New Beginnings - DND 4", 2,
            "Carillion Forest", "res://assets/music/Carillion Forest.wav"),

        ["res://assets/music/Mellyr Outpost.wav"] = new(
            "Divora", "New Beginnings - DND 4", 9,
            "Mellyr Outpost", "res://assets/music/Mellyr Outpost.wav"),

        ["res://assets/music/Drifting in the Astral Paring (Ambient).wav"] = new(
            "Divora", "Ominous Augury - DND 7", 5,
            "Drifting in the Astral Paring", "res://assets/music/Drifting in the Astral Paring (Ambient).wav"),

        ["res://assets/music/Drifting in the Astral Paring.wav"] = new(
            "Divora", "Ominous Augury - DND 7", 6,
            "Drifting in the Astral Paring", "res://assets/music/Drifting in the Astral Paring.wav"),

        ["res://assets/music/Outpacing.wav"] = new(
            "Divora", "Ominous Augury - DND 7", 8,
            "Outpacing", "res://assets/music/Outpacing.wav"),

        ["res://assets/music/Corruption Can Be Fun.wav"] = new(
            "Divora", "Ominous Augury - DND 7", 10,
            "Corruption Can Be Fun", "res://assets/music/Corruption Can Be Fun.wav"),

        ["res://assets/music/Origins Of The Gyre.wav"] = new(
            "Divora", "Origins Of The Gyre - DND 6", 1,
            "Origins Of The Gyre", "res://assets/music/Origins Of The Gyre.wav"),

        ["res://assets/music/Sozitek.wav"] = new(
            "Divora", "The Gravity Of The Situation - DND 5", 7,
            "Sozitek", "res://assets/music/Sozitek.wav"),

        ["res://assets/music/Slow Broil.wav"] = new(
            "Divora", "The Gravity Of The Situation - DND 5", 10,
            "Slow Broil", "res://assets/music/Slow Broil.wav"),
    };

    /// <summary>
    /// Look up track metadata by resource path.
    /// Returns null if the path is not in the registry.
    /// </summary>
    public static MusicTrackInfo? Lookup(string resourcePath)
    {
        return Registry.GetValueOrDefault(resourcePath);
    }

    /// <summary>Returns all registered tracks.</summary>
    public static IReadOnlyDictionary<string, MusicTrackInfo> All => Registry;
}
