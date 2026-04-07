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

    /// <summary>
    /// Multiplier applied to an encounter's selection weight based on the current
    /// weather. Used when picking one encounter from a weighted list. Encounters
    /// that list the active weather in their <c>PreferredWeather</c> array get a 2×
    /// boost; everything else stays at 1×.
    /// </summary>
    /// <param name="currentWeather">Current weather state (cast from WeatherType enum).</param>
    /// <param name="preferred">
    /// Array of weather-type ints this encounter prefers (empty = no preference).
    /// Accepts ints so the pure-logic layer stays free of the WeatherType import.
    /// </param>
    public static float WeatherWeightMultiplier(int currentWeather, int[] preferred)
    {
        if (preferred == null || preferred.Length == 0) return 1f;
        foreach (int p in preferred)
            if (p == currentWeather)
                return 2f;
        return 1f;
    }
}
