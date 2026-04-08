namespace SennenRpg.Core.Data;

/// <summary>
/// Outcome of the Rogue's "Pickpocket Combo" Fight minigame.
/// Three rapid timing windows in succession; the result depends on how many
/// the player landed and how many were dead-center perfects.
/// </summary>
public enum RogueStrikeOutcome
{
    /// <summary>No hits at all — wasted turn.</summary>
    Miss,
    /// <summary>One landing, no perfects. Resolved as a Good hit (weak).</summary>
    WeakHit,
    /// <summary>Two or more landings (or 1–2 perfects). Resolved as a Good hit.</summary>
    Good,
    /// <summary>All three landings, no perfects. Resolved as a Perfect hit, no steal.</summary>
    Perfect,
    /// <summary>All three perfects. Guaranteed crit + steals one item from the enemy's loot table.</summary>
    PerfectSteal,
}

/// <summary>
/// Pure static logic for the Rogue Fight minigame. No Godot dependency — fully NUnit-testable.
/// </summary>
public static class RogueStealLogic
{
    /// <summary>
    /// Resolve the combo result. Inputs:
    ///   <paramref name="perfectCount"/> — number of stages landed in the central sweet spot (0..3).
    ///   <paramref name="hitCount"/>     — total number of stages where the player pressed in time (0..3).
    /// Note: hitCount includes perfects (a perfect is also a hit).
    /// </summary>
    public static RogueStrikeOutcome Resolve(int perfectCount, int hitCount)
    {
        if (perfectCount < 0) perfectCount = 0;
        if (hitCount < 0) hitCount = 0;
        if (perfectCount > 3) perfectCount = 3;
        if (hitCount > 3) hitCount = 3;
        if (perfectCount > hitCount) perfectCount = hitCount;

        if (perfectCount >= 3) return RogueStrikeOutcome.PerfectSteal;
        if (hitCount >= 3)     return RogueStrikeOutcome.Perfect;
        if (hitCount >= 2)     return RogueStrikeOutcome.Good;
        if (hitCount >= 1)     return RogueStrikeOutcome.WeakHit;
        return RogueStrikeOutcome.Miss;
    }

    /// <summary>Convert the combo outcome into the existing battle <see cref="HitGrade"/>.</summary>
    public static HitGrade ToHitGrade(RogueStrikeOutcome outcome) => outcome switch
    {
        RogueStrikeOutcome.PerfectSteal => HitGrade.Perfect,
        RogueStrikeOutcome.Perfect      => HitGrade.Perfect,
        RogueStrikeOutcome.Good         => HitGrade.Good,
        RogueStrikeOutcome.WeakHit      => HitGrade.Good,
        _                               => HitGrade.Miss,
    };

    /// <summary>True when the outcome should grant a guaranteed crit on the resolved strike.</summary>
    public static bool GuaranteedCrit(RogueStrikeOutcome outcome)
        => outcome == RogueStrikeOutcome.PerfectSteal;

    /// <summary>True when the outcome should trigger an item steal from the enemy loot table.</summary>
    public static bool ShouldSteal(RogueStrikeOutcome outcome)
        => outcome == RogueStrikeOutcome.PerfectSteal;
}
