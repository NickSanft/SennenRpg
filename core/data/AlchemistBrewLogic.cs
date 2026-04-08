using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// Result of the Alchemist's "Potion Brew" Fight minigame.
/// A successful brew picks a random good outcome; a wide miss backfires onto the brewer.
/// </summary>
public enum BrewResult
{
    /// <summary>Wide miss — the brew explodes in the brewer's hand. Self-damage.</summary>
    Backfire,
    /// <summary>Narrow miss — the brew fizzles. No effect on either side.</summary>
    Neutral,
    /// <summary>Healing draught — restores HP to the brewer.</summary>
    Heal,
    /// <summary>Toxic concoction — applies Poison to the enemy.</summary>
    PoisonEnemy,
    /// <summary>Aegis tonic — applies Shield to the brewer for several turns.</summary>
    ShieldSelf,
}

/// <summary>
/// Pure static logic for the Alchemist Fight minigame. No Godot dependency — fully NUnit-testable.
/// The sweet-spot half-width scales with the player's Luck stat (per the design plan,
/// Luck is the Alchemist's signature stat).
/// </summary>
public static class AlchemistBrewLogic
{
    /// <summary>
    /// Half-width of the central sweet spot, expressed as a fraction of the bar half-width.
    /// Base 0.10, +0.005 per Luck point, capped at 0.40.
    /// At Luck 0 the central 20% of the bar is "sweet"; at Luck 60 the central ~80% is sweet.
    /// </summary>
    public static float SweetHalfWidth(int luck)
    {
        int safeLuck = Math.Max(0, luck);
        float w = 0.10f + 0.005f * safeLuck;
        if (w > 0.40f) w = 0.40f;
        return w;
    }

    /// <summary>
    /// Resolve the brew. <paramref name="accuracy"/> is the FightBar-style accuracy where
    /// 1.0 = dead-center, 0.0 = bar edge. <paramref name="roll"/> is a 0..1 random for picking
    /// which good effect to grant when the brew is sweet.
    /// </summary>
    public static BrewResult Resolve(float accuracy, int luck, float roll)
    {
        if (accuracy < 0f) accuracy = 0f;
        if (accuracy > 1f) accuracy = 1f;

        float sweet = SweetHalfWidth(luck);
        // Sweet spot covers (1 - 2*sweet) .. 1 in accuracy space because accuracy = 1 - normalisedDist.
        // So the central 2*sweet fraction of the bar produces accuracy >= (1 - 2*sweet).
        if (accuracy >= 1f - 2f * sweet)
        {
            if (roll < 0.45f) return BrewResult.Heal;
            if (roll < 0.80f) return BrewResult.PoisonEnemy;
            return BrewResult.ShieldSelf;
        }

        if (accuracy >= 0.30f) return BrewResult.Neutral;
        return BrewResult.Backfire;
    }

    /// <summary>HP restored by a Heal brew, scaled by the player's Magic stat.</summary>
    public static int HealAmount(int magic) => Math.Max(8, 10 + magic * 2);

    /// <summary>HP damage taken by the brewer when the brew backfires.</summary>
    public static int BackfireDamage(int magic) => Math.Max(2, magic / 2 + 3);

    /// <summary>Turns of Poison applied to the enemy on a PoisonEnemy outcome.</summary>
    public const int PoisonTurns = 3;

    /// <summary>Turns of Shield applied to the brewer on a ShieldSelf outcome.</summary>
    public const int ShieldTurns = 3;
}
