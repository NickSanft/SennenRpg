using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static helpers for advancing a <see cref="BestiaryEntry"/>.
/// Mirrors <see cref="ForageCodexLogic.RecordFind"/> in shape and intent — callers
/// pass in the existing entry (or null), the timestamp, and get back the next state.
/// </summary>
public static class BestiaryRecordLogic
{
    /// <summary>
    /// Records a kill at the given UTC timestamp.
    /// First-defeated timestamp is set on the very first call and is **never**
    /// overwritten on subsequent kills, mirroring the foraging codex's first-found
    /// behavior.
    /// </summary>
    public static BestiaryEntry RecordKill(BestiaryEntry? existing, DateTime nowUtc)
    {
        if (existing == null)
        {
            return new BestiaryEntry
            {
                FirstDefeatedUtc = nowUtc,
                TotalKills       = 1,
            };
        }
        return existing with { TotalKills = existing.TotalKills + 1 };
    }
}
