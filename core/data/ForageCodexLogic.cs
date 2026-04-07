using System;
using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static helpers for the Foragery codex.
/// Owns no state — callers pass in (and re-store) the dictionary.
/// All methods are deterministic and timestamp-injectable for testing.
/// </summary>
public static class ForageCodexLogic
{
    /// <summary>
    /// Records that the player just foraged <paramref name="itemPath"/> with grade <paramref name="grade"/>
    /// at moment <paramref name="nowUtc"/>. Returns the updated entry — callers should
    /// re-assign it back into their dictionary.
    ///
    /// First-found timestamp is set on the first record and never overwritten.
    /// Best grade is taken as the maximum of the existing best and the new grade.
    /// </summary>
    public static ForageCodexEntry RecordFind(
        ForageCodexEntry? existing,
        ForageLogic.ForageGrade grade,
        DateTime nowUtc)
    {
        if (existing == null)
        {
            return new ForageCodexEntry
            {
                FirstFoundUtc = nowUtc,
                TimesFound    = 1,
                BestGradeRaw  = (int)grade,
            };
        }

        int bestGrade = Math.Max(existing.BestGradeRaw, (int)grade);
        return existing with
        {
            TimesFound   = existing.TimesFound + 1,
            BestGradeRaw = bestGrade,
        };
    }

    /// <summary>
    /// True when the player has ever foraged this item. Locked entries render as
    /// silhouettes in the codex.
    /// </summary>
    public static bool IsUnlocked(IReadOnlyDictionary<string, ForageCodexEntry> entries, string itemPath)
        => entries.ContainsKey(itemPath);

    /// <summary>
    /// Returns codex entries sorted alphabetically by the supplied display-name resolver.
    /// <paramref name="displayNameOf"/> is provided as a delegate so the pure-logic layer
    /// stays free of Godot resource loading.
    /// </summary>
    public static List<KeyValuePair<string, ForageCodexEntry>> EntriesSortedAlphabetical(
        IReadOnlyDictionary<string, ForageCodexEntry> entries,
        Func<string, string> displayNameOf)
    {
        return entries
            .OrderBy(kv => displayNameOf(kv.Key), StringComparer.OrdinalIgnoreCase)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Total items recorded across all entries (sum of <see cref="ForageCodexEntry.TimesFound"/>).
    /// </summary>
    public static int TotalFinds(IReadOnlyDictionary<string, ForageCodexEntry> entries)
    {
        int total = 0;
        foreach (var (_, entry) in entries)
            total += entry.TimesFound;
        return total;
    }
}
