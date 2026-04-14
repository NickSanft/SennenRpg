namespace SennenRpg.Core.Data;

/// <summary>
/// Pure logic for the per-actor unique Skills introduced in Phase 4.
/// No Godot runtime — fully covered by NUnit.
///
/// Skills:
/// - Bhata "Gravity Arrow"   — Ranger Aim minigame, ×2 base damage, 6 MP
/// - Rain  "Dual-Class"      — Ranger Aim minigame, ×2 base damage, 6 MP
/// - Lily   "Wither and Bloom" — hold-the-button bloom, deals damage to a target
///                               and splits a heal across the living party. 8 MP.
/// - Kriora "Crystal Knife"    — MageRuneInput minigame, ×2 magic damage to ALL enemies. 10 MP.
/// </summary>
public static class SkillResolver
{
    public const int GravityArrowMpCost   = 6;
    public const int DualClassMpCost      = 6;
    public const int WitherAndBloomMpCost = 8;
    public const int CrystalKnifeMpCost   = 10;

    public const float GravityArrowMultiplier = 2.0f;
    public const float DualClassMultiplier    = 2.0f;
    public const float CrystalKnifeMultiplier = 2.0f;

    /// <summary>
    /// Damage dealt by a Ranger-aim style skill that doubles the base attack.
    /// Defence is subtracted (clamped to 1) and an accuracy multiplier (0..1) is applied.
    /// </summary>
    public static int ResolveRangerSkillDamage(int actorAttack, int targetDefence, float accuracy, float multiplier)
    {
        if (multiplier < 0f) multiplier = 0f;
        if (accuracy   < 0f) accuracy   = 0f;
        if (accuracy   > 1f) accuracy   = 1f;
        int raw  = (int)((actorAttack * multiplier) - targetDefence);
        if (raw < 1) raw = 1;
        int dealt = (int)(raw * accuracy);
        return dealt < 1 ? 1 : dealt;
    }

    /// <summary>
    /// "Wither and Bloom" damage. Magic-based with a fillRatio scalar (how full the bloom got).
    /// Defence is subtracted (clamped to 1).
    /// </summary>
    public static int ResolveWitherDamage(int actorMagic, int targetDefence, float fillRatio)
    {
        if (fillRatio < 0f) fillRatio = 0f;
        if (fillRatio > 1f) fillRatio = 1f;
        int raw = (int)(actorMagic * (0.75f + fillRatio)) - targetDefence;
        if (raw < 1) raw = 1;
        return raw;
    }

    /// <summary>
    /// "Wither and Bloom" heal pool size for the bloom phase. Scales with magic and fillRatio.
    /// </summary>
    public static int ResolveWitherHealPool(int actorMagic, float fillRatio)
    {
        if (fillRatio < 0f) fillRatio = 0f;
        if (fillRatio > 1f) fillRatio = 1f;
        int pool = (int)(actorMagic * (0.5f + fillRatio));
        return pool < 1 ? 1 : pool;
    }

    /// <summary>
    /// Splits a heal pool evenly across the given number of living members.
    /// Returns the per-member heal amount (rounded down, minimum 1 if any pool exists).
    /// </summary>
    public static int SplitHealEvenly(int healPool, int livingMemberCount)
    {
        if (livingMemberCount <= 0 || healPool <= 0) return 0;
        int per = healPool / livingMemberCount;
        return per < 1 ? 1 : per;
    }

    /// <summary>
    /// Crystal Knife damage per enemy. Magic-based with a 2× multiplier.
    /// The MageRuneInput correctCount (0..3) maps to an accuracy scalar.
    /// Hits ALL enemies — the caller loops over the enemy list.
    /// </summary>
    /// <summary>
    /// Estimate damage range for a Ranger-aim style skill (Dual-Class, Gravity Arrow).
    /// Low = poor-accuracy (0.2 scalar), High = perfect aim (1.0).
    /// </summary>
    public static (int minDmg, int maxDmg) EstimateRangerSkillDamageRange(int actorAttack, int targetDefence, float multiplier)
    {
        int low  = ResolveRangerSkillDamage(actorAttack, targetDefence, 0.2f, multiplier);
        int high = ResolveRangerSkillDamage(actorAttack, targetDefence, 1.0f, multiplier);
        if (high < low) high = low;
        return (low, high);
    }

    /// <summary>
    /// Estimate damage range for Lily's Wither and Bloom (magic-scaled, fillRatio 0..1).
    /// </summary>
    public static (int minDmg, int maxDmg) EstimateWitherDamageRange(int actorMagic, int targetDefence)
    {
        int low  = ResolveWitherDamage(actorMagic, targetDefence, 0f);
        int high = ResolveWitherDamage(actorMagic, targetDefence, 1f);
        if (high < low) high = low;
        return (low, high);
    }

    /// <summary>
    /// Estimate per-enemy damage range for Crystal Knife (magic AoE, correctCount 0..3).
    /// </summary>
    public static (int minDmg, int maxDmg) EstimateCrystalKnifeDamageRange(int actorMagic, int targetDefence)
    {
        int low  = ResolveCrystalKnifeDamage(actorMagic, targetDefence, 0);
        int high = ResolveCrystalKnifeDamage(actorMagic, targetDefence, 3);
        if (high < low) high = low;
        return (low, high);
    }

    public static int ResolveCrystalKnifeDamage(int actorMagic, int targetDefence, int correctCount)
    {
        float accuracy = correctCount switch
        {
            >= 3 => 1.0f,
               2 => 0.8f,
               1 => 0.6f,
               _ => 0.4f,
        };
        int raw = (int)(actorMagic * CrystalKnifeMultiplier * accuracy) - targetDefence;
        return raw < 1 ? 1 : raw;
    }
}
