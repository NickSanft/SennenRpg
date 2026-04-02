using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// Per-enemy performance history accumulated across encounters.
/// Persisted in SaveData keyed by EnemyData.EnemyId.
/// </summary>
public record EnemyRhythmHistory(
    int TotalEncounters,
    int TotalPerfects,
    int TotalGoods,
    int TotalMisses,
    int BestMaxStreak);

/// <summary>How the enemy has adapted to the player's skill level.</summary>
public enum AdaptationTier { Cocky, None, Wary, Hardened, Rival }

/// <summary>Computed adaptation values applied to a single battle.</summary>
public record AdaptationResult(
    AdaptationTier Tier,
    int ExtraMeasures,
    float ObstacleDensityMult,
    float BonusGoldPercent,
    float BonusExpPercent,
    float BonusLootChance);

/// <summary>
/// Pure-static logic for the Rhythm Memory system.
/// Enemies remember how well the player performs and adapt accordingly.
/// All methods are side-effect-free and safe to call in NUnit tests.
/// </summary>
public static class RhythmMemoryLogic
{
    /// <summary>Minimum encounters before adaptation activates.</summary>
    public const int EncounterThreshold = 3;

    private static readonly AdaptationResult CockyResult    = new(AdaptationTier.Cocky,    -1, 0.50f, 0f,    0f,    0f);
    private static readonly AdaptationResult NoneResult     = new(AdaptationTier.None,      0, 1.00f, 0f,    0f,    0f);
    private static readonly AdaptationResult WaryResult     = new(AdaptationTier.Wary,      0, 1.00f, 0.10f, 0.10f, 0.05f);
    private static readonly AdaptationResult HardenedResult = new(AdaptationTier.Hardened,  1, 1.25f, 0.25f, 0.20f, 0.15f);
    private static readonly AdaptationResult RivalResult    = new(AdaptationTier.Rival,     2, 1.50f, 0.50f, 0.40f, 0.30f);

    /// <summary>
    /// Computes the adaptation for a battle based on accumulated history.
    /// Returns <see cref="AdaptationTier.None"/> with default values if history
    /// is null or below the encounter threshold.
    /// </summary>
    public static AdaptationResult ComputeAdaptation(EnemyRhythmHistory? history)
    {
        if (history == null || history.TotalEncounters < EncounterThreshold)
            return NoneResult;

        int total = history.TotalPerfects + history.TotalGoods + history.TotalMisses;
        if (total == 0)
            return NoneResult;

        float perfectRate = (float)history.TotalPerfects / total;
        float missRate    = (float)history.TotalMisses   / total;

        // Cocky: enemy underestimates the player
        if (perfectRate < 0.15f && missRate > 0.60f)
            return CockyResult;

        // Adaptation tiers based on how consistently the player hits Perfect
        if (perfectRate >= 0.80f) return RivalResult;
        if (perfectRate >= 0.55f) return HardenedResult;
        if (perfectRate >= 0.30f) return WaryResult;

        return NoneResult;
    }

    /// <summary>
    /// Merges a completed battle's performance into the enemy's history.
    /// If <paramref name="existing"/> is null, creates a fresh history.
    /// </summary>
    public static EnemyRhythmHistory RecordEncounter(
        EnemyRhythmHistory? existing, PerformanceScore current)
    {
        int encounters = (existing?.TotalEncounters ?? 0) + 1;
        int perfects   = (existing?.TotalPerfects   ?? 0) + current.Perfects;
        int goods      = (existing?.TotalGoods      ?? 0) + current.Goods;
        int misses     = (existing?.TotalMisses     ?? 0) + current.Misses;
        int bestStreak = Math.Max(existing?.BestMaxStreak ?? 0, current.MaxStreak);

        return new EnemyRhythmHistory(encounters, perfects, goods, misses, bestStreak);
    }
}
