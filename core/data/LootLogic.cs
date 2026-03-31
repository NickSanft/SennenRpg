using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure weighted-loot logic. No Godot runtime dependency — fully unit-testable with NUnit.
/// All methods work on plain parallel arrays so they can be called from tests without
/// instantiating Godot <see cref="LootEntry"/> resources.
/// </summary>
public static class LootLogic
{
    /// <summary>
    /// Selects one item path from a weighted loot table.
    /// </summary>
    /// <param name="itemPaths">Parallel array of item resource paths.</param>
    /// <param name="weights">Parallel array of integer weights (ignored for guaranteed entries).</param>
    /// <param name="guaranteed">Parallel array of guaranteed flags.</param>
    /// <param name="rng">Function returning a uniform random float in [0, 1).</param>
    /// <returns>
    /// The selected item path, or <c>null</c> if the table is empty or all weights are zero.
    /// Guaranteed entries win immediately (first guaranteed entry in the array wins).
    /// </returns>
    public static string? RollLoot(string[] itemPaths, int[] weights, bool[] guaranteed, Func<float> rng)
    {
        int len = itemPaths.Length;
        if (len == 0) return null;

        // Guaranteed entries bypass the weighted roll entirely.
        for (int i = 0; i < len; i++)
            if (i < guaranteed.Length && guaranteed[i]) return itemPaths[i];

        // Weighted random selection.
        int total = TotalWeight(weights, guaranteed);
        if (total <= 0) return null;

        float roll = rng() * total;
        float acc  = 0f;
        for (int i = 0; i < len; i++)
        {
            int w = i < weights.Length ? weights[i] : 0;
            if (w <= 0) continue;
            acc += w;
            if (roll < acc) return itemPaths[i];
        }

        // Floating-point edge case: return the last positive-weight entry.
        for (int i = len - 1; i >= 0; i--)
            if (i < weights.Length && weights[i] > 0) return itemPaths[i];

        return null;
    }

    /// <summary>
    /// Returns the sum of all non-guaranteed, positive weights in the table.
    /// </summary>
    public static int TotalWeight(int[] weights, bool[] guaranteed)
    {
        int total = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (i < guaranteed.Length && guaranteed[i]) continue;
            if (weights[i] > 0) total += weights[i];
        }
        return total;
    }

    /// <summary>
    /// Returns the probability (0–1) of each entry being selected by the weighted roll,
    /// treating guaranteed entries as probability 1 and skipping all others.
    /// Useful for debug display or tests.
    /// </summary>
    public static float[] ComputeProbabilities(int[] weights, bool[] guaranteed)
    {
        int   len   = weights.Length;
        var   probs = new float[len];
        int   total = TotalWeight(weights, guaranteed);

        for (int i = 0; i < len; i++)
        {
            if (i < guaranteed.Length && guaranteed[i])
                probs[i] = 1f;
            else if (total > 0 && weights[i] > 0)
                probs[i] = (float)weights[i] / total;
        }
        return probs;
    }
}
