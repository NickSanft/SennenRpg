using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure foraging logic — no Godot runtime dependency.
/// All methods are static and side-effect-free so they can be unit-tested with NUnit.
/// </summary>
public static class ForageLogic
{
    /// <summary>Default forage table. Cheaper items are more common.</summary>
    public static readonly ForageTableEntry[] DefaultTable =
    [
        new("res://resources/items/junk_anima_slug_slime.tres", 40),
        new("res://resources/items/junk_flopsin_hairball.tres", 30),
        new("res://resources/items/junk_gravi_shard.tres",      20),
        new("res://resources/items/junk_astral_flower.tres",    10),
    ];

    /// <summary>
    /// Returns true when the player should forage this step.
    /// <paramref name="roll"/> should be in [0, 100). Returns true when roll &lt; chancePercent.
    /// </summary>
    public static bool ShouldForage(double roll, double chancePercent = 5.0)
        => roll < chancePercent;

    /// <summary>
    /// Selects an item from the weighted forage table.
    /// <paramref name="roll"/> should be in [0.0, 1.0).
    /// Returns the resource path of the selected item.
    /// </summary>
    public static string SelectForageItem(double roll, ForageTableEntry[] table)
    {
        if (table.Length == 0)
            throw new ArgumentException("Forage table must not be empty.", nameof(table));

        int totalWeight = 0;
        foreach (var entry in table)
            totalWeight += entry.Weight;

        double cumulative = 0.0;
        foreach (var entry in table)
        {
            cumulative += (double)entry.Weight / totalWeight;
            if (roll < cumulative)
                return entry.ItemPath;
        }

        // Fallback for floating-point edge case (roll ~= 1.0)
        return table[^1].ItemPath;
    }

    // ── Rhythm Foraging Minigame Grading ──────────────────────────────────────

    /// <summary>
    /// Performance grade for the foraging rhythm minigame.
    /// Drives bonus item count and table biasing.
    /// </summary>
    public enum ForageGrade { Miss, Good, Great, Perfect }

    /// <summary>
    /// Compute a grade from minigame results.
    /// <paramref name="hits"/> is total notes hit (any quality).
    /// <paramref name="perfects"/> is the subset of those hits that landed in the Perfect window.
    /// <paramref name="totalNotes"/> is the full note count (must be &gt; 0).
    ///
    /// Thresholds:
    ///   Perfect → all notes hit AND every hit was perfect
    ///   Great   → ≥ 80% notes hit AND ≥ 50% of hits were perfect
    ///   Good    → ≥ 50% notes hit
    ///   Miss    → otherwise (legacy fallback — never worse than the no-minigame baseline)
    /// </summary>
    public static ForageGrade GradeFromAccuracy(int hits, int perfects, int totalNotes)
    {
        if (totalNotes <= 0) return ForageGrade.Miss;
        if (hits < 0) hits = 0;
        if (perfects < 0) perfects = 0;
        if (perfects > hits) perfects = hits;

        double hitRatio     = (double)hits     / totalNotes;
        double perfectRatio = hits == 0 ? 0.0 : (double)perfects / hits;

        if (hits == totalNotes && perfects == totalNotes)
            return ForageGrade.Perfect;
        if (hitRatio >= 0.80 && perfectRatio >= 0.50)
            return ForageGrade.Great;
        if (hitRatio >= 0.50)
            return ForageGrade.Good;
        return ForageGrade.Miss;
    }

    /// <summary>
    /// Number of items granted for a given grade.
    /// Miss returns 1 so the player is never worse off than the legacy instant-grant.
    /// </summary>
    public static int BonusItemCount(ForageGrade grade) => grade switch
    {
        ForageGrade.Perfect => 3,
        ForageGrade.Great   => 2,
        ForageGrade.Good    => 1,
        _                   => 1, // Miss
    };

    /// <summary>
    /// Returns a forage table biased toward rarer entries based on grade.
    /// Miss/Good return the unmodified <see cref="DefaultTable"/>.
    /// Great doubles the weight of the rarest half (gravi shard, astral flower).
    /// Perfect triples the rarest entry's weight on top of the Great bias.
    ///
    /// Always returns a fresh array so callers can mutate without poisoning the default.
    /// </summary>
    public static ForageTableEntry[] WeightedTableForGrade(ForageGrade grade)
        => WeightedTableForGrade(grade, DefaultTable);

    /// <summary>
    /// Overload that takes an explicit base table — used by tests and by future
    /// region-specific tables. The base table is treated as ordered from most-common
    /// (index 0) to rarest (last index), matching <see cref="DefaultTable"/>.
    /// </summary>
    public static ForageTableEntry[] WeightedTableForGrade(ForageGrade grade, ForageTableEntry[] baseTable)
    {
        if (baseTable.Length == 0)
            throw new ArgumentException("Base table must not be empty.", nameof(baseTable));

        // Copy entries so we can rewrite weights without touching the source.
        var result = new ForageTableEntry[baseTable.Length];
        for (int i = 0; i < baseTable.Length; i++)
            result[i] = baseTable[i];

        if (grade == ForageGrade.Miss || grade == ForageGrade.Good)
            return result;

        // Great: double the rarest half (back of the array).
        int rareStart = result.Length / 2;
        for (int i = rareStart; i < result.Length; i++)
            result[i] = result[i] with { Weight = result[i].Weight * 2 };

        if (grade == ForageGrade.Perfect)
        {
            // Perfect: stack an additional ×1.5 (rounded up) on the very rarest entry.
            int last = result.Length - 1;
            int boosted = (int)Math.Ceiling(result[last].Weight * 1.5);
            result[last] = result[last] with { Weight = boosted };
        }

        return result;
    }

    /// <summary>Display label for a forage grade — used in dialog and minigame UI.</summary>
    public static string GradeLabel(ForageGrade grade) => grade switch
    {
        ForageGrade.Perfect => "Perfect!",
        ForageGrade.Great   => "Great!",
        ForageGrade.Good    => "Good",
        _                   => "Miss",
    };

    /// <summary>
    /// Article/qualifier inserted into the dialog line, e.g. "a perfect", "a great", "a fine", "a".
    /// Used by forage_found.dtl as the {forage_grade} variable.
    /// </summary>
    public static string GradeArticle(ForageGrade grade) => grade switch
    {
        ForageGrade.Perfect => "a perfect",
        ForageGrade.Great   => "a great",
        ForageGrade.Good    => "a fine",
        _                   => "a",
    };
}
