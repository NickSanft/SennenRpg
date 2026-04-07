using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// One bestiary entry — recorded the first time the player defeats an enemy and
/// updated on every subsequent kill. Lives in <see cref="BestiaryData.Entries"/>
/// keyed by <c>EnemyData.EnemyId</c>.
/// </summary>
public record BestiaryEntry
{
    /// <summary>UTC timestamp of the very first defeat. Never overwritten on later kills.</summary>
    public DateTime FirstDefeatedUtc { get; init; }

    /// <summary>
    /// Total kills recorded in this entry. Mirrors <c>GameManager.KillCounts</c>
    /// but is stored separately so the bestiary survives any future schema changes
    /// to the older int-dict.
    /// </summary>
    public int TotalKills { get; init; }
}
