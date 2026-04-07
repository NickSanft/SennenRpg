using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// One entry in the Foragery codex — tracks the player's history with a single forageable item.
/// Stored under <see cref="ForageCodexData.Entries"/> keyed by item resource path.
/// </summary>
public record ForageCodexEntry
{
    /// <summary>UTC timestamp of the very first time this item was foraged.</summary>
    public DateTime FirstFoundUtc { get; init; }

    /// <summary>Total number of times this item has been foraged across all minigames.</summary>
    public int TimesFound { get; init; }

    /// <summary>
    /// Best grade ever achieved on the forage minigame run that produced this item.
    /// Stored as an int so it can serialise cleanly through System.Text.Json with no
    /// converter — cast back to <see cref="ForageLogic.ForageGrade"/> on read.
    /// </summary>
    public int BestGradeRaw { get; init; }
}
