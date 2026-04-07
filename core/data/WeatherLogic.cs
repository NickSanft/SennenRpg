using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static logic for the weather system — no Godot dependency.
/// Holds step-interval rolls, transition selection, BGM path lookup, and helpers for
/// rendering / cross-system coupling.
/// </summary>
public static class WeatherLogic
{
    /// <summary>
    /// Default steps between weather rolls on the world map.
    /// Tuned so the first weather change feels lively but not jittery — combined with
    /// the Sunny row's ~54% stay-Sunny weight in <see cref="WeatherTransitionTable.Default"/>,
    /// the expected wait for the first non-Sunny state is ~40–50 steps.
    /// </summary>
    public const int DefaultRollInterval = 20;

    // ── Weather-specific BGM paths (registered in MusicMetadata). ────────
    public const string ForggyBgmPath  = "res://assets/music/Foggy Morning in Flas.wav";
    public const string StormyBgmPath  = "res://assets/music/A Calm Rain in Argyre.wav";
    public const string SnowyBgmPath   = "res://assets/music/Glimmersong Forest.wav";
    /// <summary>Aurora hijacks an existing track for its rare moments.</summary>
    public const string AuroraBgmPath  = "res://assets/music/Origins Of The Gyre.wav";

    /// <summary>
    /// True when the step counter has reached the next roll boundary.
    /// Fires at step <paramref name="interval"/>, 2×interval, … (never at 0).
    /// </summary>
    public static bool ShouldRoll(int stepCounter, int interval)
        => stepCounter > 0 && interval > 0 && stepCounter % interval == 0;

    /// <summary>
    /// Chooses the next weather state for the given current weather using a roll in [0, 1).
    /// Walks the row of the transition matrix cumulatively and returns the first state
    /// whose cumulative probability exceeds <paramref name="roll"/>.
    /// </summary>
    public static WeatherType RollNext(
        WeatherType current,
        double roll,
        WeatherTransitionTable table)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));

        double cumulative = 0.0;
        for (int to = 0; to < 5; to++)
        {
            cumulative += table.Matrix[(int)current, to];
            if (roll < cumulative)
                return (WeatherType)to;
        }
        // Floating-point safety net: roll ~= 1.0.
        return current;
    }

    /// <summary>
    /// Returns the BGM path to play under this weather. Sunny (and any unknown value)
    /// returns <paramref name="sunnyBgmPath"/> so the map's default track keeps playing.
    /// </summary>
    public static string BgmPathFor(WeatherType weather, string sunnyBgmPath) => weather switch
    {
        WeatherType.Foggy  => ForggyBgmPath,
        WeatherType.Stormy => StormyBgmPath,
        WeatherType.Snowy  => SnowyBgmPath,
        WeatherType.Aurora => AuroraBgmPath,
        _                  => sunnyBgmPath,
    };

    /// <summary>Human-readable label for UI (e.g. area name tint, debug peek).</summary>
    public static string DisplayName(WeatherType weather) => weather switch
    {
        WeatherType.Sunny  => "Clear Skies",
        WeatherType.Foggy  => "Foggy",
        WeatherType.Stormy => "Stormy",
        WeatherType.Snowy  => "Snowy",
        WeatherType.Aurora => "Aurora",
        _                  => "Unknown",
    };

    // ── Feature 1 ↔ Feature 2 coupling ───────────────────────────────────
    // Weather-aware overload of the foraging table bias. Mirrors the day/night
    // overload in ForageLogic so rare drops favor certain weathers.

    /// <summary>
    /// Applies weather bias to a forage table already biased for
    /// grade and day/night. Mutates a copy — safe to call with the result of
    /// <see cref="ForageLogic.WeightedTableForGrade(ForageLogic.ForageGrade, ForageLogic.DayPhase)"/>.
    ///
    /// Rules:
    ///   Stormy → double slime weight (wet weather creatures)
    ///   Foggy  → double rare-half weights (fog = mystery)
    ///   Snowy  → halve common weights, slightly boost rarest entry
    ///   Aurora → triple rare-half weights AND boost rarest entry ×2
    ///   Sunny  → unchanged
    /// </summary>
    public static ForageTableEntry[] ApplyWeatherBias(
        ForageTableEntry[] baseTable,
        WeatherType weather)
    {
        if (baseTable == null) throw new ArgumentNullException(nameof(baseTable));
        if (baseTable.Length == 0) return baseTable;

        // Always return a copy so callers never mutate the source.
        var result = new ForageTableEntry[baseTable.Length];
        for (int i = 0; i < baseTable.Length; i++)
            result[i] = baseTable[i];

        int half = result.Length / 2;
        int last = result.Length - 1;

        switch (weather)
        {
            case WeatherType.Stormy:
                // Slime is index 0 in the default table.
                result[0] = result[0] with { Weight = result[0].Weight * 2 };
                break;

            case WeatherType.Foggy:
                for (int i = half; i < result.Length; i++)
                    result[i] = result[i] with { Weight = result[i].Weight * 2 };
                break;

            case WeatherType.Snowy:
                for (int i = 0; i < half; i++)
                    result[i] = result[i] with { Weight = Math.Max(1, result[i].Weight / 2) };
                result[last] = result[last] with { Weight = result[last].Weight + 5 };
                break;

            case WeatherType.Aurora:
                for (int i = half; i < result.Length; i++)
                    result[i] = result[i] with { Weight = result[i].Weight * 3 };
                result[last] = result[last] with { Weight = result[last].Weight * 2 };
                break;

            case WeatherType.Sunny:
            default:
                // No change.
                break;
        }

        return result;
    }

    /// <summary>
    /// Stat buff multiplier (e.g. Aurora grants a +5% all-stat boost while active).
    /// Returns 1.0 when no buff applies.
    /// </summary>
    public static float StatBuffMultiplier(WeatherType weather) => weather switch
    {
        WeatherType.Aurora => 1.05f,
        _                  => 1.00f,
    };

    /// <summary>
    /// Lightning strike roll — only meaningful during <see cref="WeatherType.Stormy"/>.
    /// Returns true when <paramref name="roll"/> (in [0, 100)) is below 0.5%.
    /// Callers should gate on weather == Stormy before even rolling.
    /// </summary>
    public static bool ShouldStrikeLightning(double roll)
        => roll < 0.5;
}
