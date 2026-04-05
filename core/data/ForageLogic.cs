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
}
