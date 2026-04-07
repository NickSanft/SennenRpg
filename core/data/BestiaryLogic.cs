using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static logic for the Bestiary menu — tier gating, mastery ranks, completion math.
/// No Godot dependency, fully NUnit-testable.
/// </summary>
public static class BestiaryLogic
{
    /// <summary>Kill count required to unlock the drop table on a bestiary entry.</summary>
    public const int DropRevealThreshold = 3;

    /// <summary>Tier of unlock for a single bestiary entry.</summary>
    public enum EntryTier
    {
        /// <summary>Player has never defeated this enemy. Renders as silhouette + ???.</summary>
        Locked,
        /// <summary>Defeated 1–2 times. Name, sprite, flavor text, and combat stats visible.</summary>
        Discovered,
        /// <summary>Defeated 3+ times. Drop tables, rates, and Bardic skill list also visible.</summary>
        Studied,
    }

    /// <summary>Returns the unlock tier for the given kill count.</summary>
    public static EntryTier TierFor(int killCount)
    {
        if (killCount <= 0) return EntryTier.Locked;
        if (killCount < DropRevealThreshold) return EntryTier.Discovered;
        return EntryTier.Studied;
    }

    /// <summary>
    /// Number of additional kills required before drops are revealed.
    /// Returns 0 for already-studied entries.
    /// </summary>
    public static int KillsUntilStudied(int killCount)
    {
        int delta = DropRevealThreshold - killCount;
        return delta < 0 ? 0 : delta;
    }

    /// <summary>
    /// Returns (discovered, total) over <paramref name="allEnemyIds"/>.
    /// "Discovered" = at least one kill recorded in <paramref name="killCounts"/>.
    /// </summary>
    public static (int discovered, int total) Completion(
        IEnumerable<string> allEnemyIds,
        IReadOnlyDictionary<string, int> killCounts)
    {
        int total = 0, discovered = 0;
        foreach (var id in allEnemyIds)
        {
            total++;
            if (killCounts.TryGetValue(id, out int n) && n > 0)
                discovered++;
        }
        return (discovered, total);
    }

    /// <summary>
    /// Pokédex-style mastery rank awarded purely for kill count. Used as a small
    /// star indicator next to the entry list.
    /// </summary>
    public enum MasteryRank
    {
        None     = 0,
        Bronze   = 1,
        Silver   = 2,
        Gold     = 3,
        Platinum = 4,
    }

    /// <summary>
    /// Mastery rank thresholds: 3 = Bronze, 10 = Silver, 25 = Gold, 50 = Platinum.
    /// Below 3 → None (no rank shown).
    /// </summary>
    public static MasteryRank MasteryFor(int killCount)
    {
        if (killCount >= 50) return MasteryRank.Platinum;
        if (killCount >= 25) return MasteryRank.Gold;
        if (killCount >= 10) return MasteryRank.Silver;
        if (killCount >= 3)  return MasteryRank.Bronze;
        return MasteryRank.None;
    }
}
