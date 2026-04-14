namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-logic helpers for multi-slot save files. No Godot runtime dependency —
/// safe to reference from NUnit tests.
/// </summary>
public static class SaveSlotLogic
{
    public const int MaxSlots = 3;

    /// <summary>Returns the user:// path for the given save slot (1-based).</summary>
    public static string GetSavePath(int slot) => $"user://save_{slot}.json";

    /// <summary>Returns true when <paramref name="slot"/> is within the valid 1-based range.</summary>
    public static bool IsValidSlot(int slot) => slot >= 1 && slot <= MaxSlots;

    /// <summary>
    /// Formats a total second count as a human-readable duration string.
    /// Examples: 45 → "45s", 90 → "1m 30s", 3661 → "1h 01m".
    /// </summary>
    public static string FormatPlayTime(int totalSeconds)
    {
        if (totalSeconds < 0) totalSeconds = 0;
        if (totalSeconds < 60) return $"{totalSeconds}s";

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        int hours   = minutes   / 60;
        minutes     = minutes   % 60;

        return hours > 0
            ? $"{hours}h {minutes:D2}m"
            : $"{minutes}m {seconds:D2}s";
    }
}

/// <summary>Metadata read from a save file for display on the slot-picker screen.</summary>
public record SaveSlotInfo(
    int    Level,
    string PlayerName,
    int    PlayTimeSeconds,
    string Timestamp,
    string ClassName = "Bard",
    string MapName   = "Unknown"
);
