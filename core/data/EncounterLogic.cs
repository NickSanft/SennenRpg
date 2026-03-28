namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static encounter rate formulas — no Godot dependency, NUnit-testable.
///
/// Per-step encounter probability:
///   baseRate     = 0.10  (10 % per step)
///   luckFactor   = clamp(1 - luck × 0.04,  minFactor=0.15, 1.0)
///   nightFactor  = isNight ? 1.60 : 1.00
///   finalRate    = baseRate × luckFactor × nightFactor
/// </summary>
public static class EncounterLogic
{
    public const float BaseRate        = 0.10f;
    public const float NightMultiplier = 1.60f;
    public const float MinLuckFactor   = 0.15f;
    public const float LuckStep        = 0.04f;

    /// <summary>
    /// Returns the per-step encounter probability (0–1).
    /// Higher luck → fewer encounters. Night → more encounters.
    /// </summary>
    public static float EncounterRate(int luck, bool isNight)
    {
        float luckFactor  = MathF.Max(MinLuckFactor, 1f - luck * LuckStep);
        float nightFactor = isNight ? NightMultiplier : 1f;
        return BaseRate * luckFactor * nightFactor;
    }
}
