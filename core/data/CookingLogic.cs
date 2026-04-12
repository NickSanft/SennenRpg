using System;
using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure static cooking calculations — no Godot runtime dependency.
/// All methods are static and side-effect-free so they can be unit-tested with NUnit.
/// </summary>
public static class CookingLogic
{
    /// <summary>
    /// Check if the player has all required ingredients in their inventory.
    /// </summary>
    public static bool HasIngredients(
        IEnumerable<string> inventoryPaths,
        RecipeIngredient[] ingredients)
    {
        var available = CountPaths(inventoryPaths);
        foreach (var ing in ingredients)
        {
            if (!available.TryGetValue(ing.ItemPath, out int count) || count < ing.Count)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns remaining inventory paths after removing consumed ingredients.
    /// Returns null if ingredients are insufficient.
    /// </summary>
    public static List<string>? ConsumeIngredients(
        IEnumerable<string> inventoryPaths,
        RecipeIngredient[] ingredients)
    {
        var remaining = new List<string>(inventoryPaths);
        foreach (var ing in ingredients)
        {
            for (int i = 0; i < ing.Count; i++)
            {
                int idx = remaining.IndexOf(ing.ItemPath);
                if (idx < 0) return null;
                remaining.RemoveAt(idx);
            }
        }
        return remaining;
    }

    /// <summary>
    /// Determine cooking quality from minigame performance.
    /// Perfect: >= 80% hit AND >= 50% of hits are Perfect-grade.
    /// Normal: >= 50% hit.
    /// Burnt: less than 50% hit.
    /// When <paramref name="hasBrewMaster"/> is true (Lily Lv15 milestone),
    /// thresholds are relaxed: Perfect needs 70% hit and 40% perfect ratio.
    /// </summary>
    public static CookingQuality DetermineQuality(
        int perfectCount, int goodCount, int totalNotes, bool hasBrewMaster = false)
    {
        if (totalNotes <= 0) return CookingQuality.Burnt;

        int totalHits = perfectCount + goodCount;
        float hitRate = (float)totalHits / totalNotes;

        float perfectHitThreshold   = hasBrewMaster ? 0.70f : 0.80f;
        float perfectRatioThreshold = hasBrewMaster ? 0.40f : 0.50f;

        if (hitRate >= perfectHitThreshold && totalHits > 0
            && (float)perfectCount / totalHits >= perfectRatioThreshold)
            return CookingQuality.Perfect;
        if (hitRate >= 0.5f)
            return CookingQuality.Normal;
        return CookingQuality.Burnt;
    }

    /// <summary>
    /// Apply quality multiplier to base heal: Burnt=0.5x, Normal=1.0x, Perfect=1.5x.
    /// </summary>
    public static int QualityHealBonus(int baseHealAmount, CookingQuality quality)
    {
        return quality switch
        {
            CookingQuality.Burnt   => (int)(baseHealAmount * 0.5f),
            CookingQuality.Normal  => baseHealAmount,
            CookingQuality.Perfect => (int)(baseHealAmount * 1.5f),
            _ => baseHealAmount,
        };
    }

    /// <summary>
    /// Derive the quality-variant resource path from the base output path.
    /// Normal uses the base path unchanged; Burnt/Perfect append a suffix before .tres.
    /// </summary>
    public static string QualityItemPath(string baseOutputPath, CookingQuality quality)
    {
        if (quality == CookingQuality.Normal)
            return baseOutputPath;

        string suffix = quality == CookingQuality.Burnt ? "_burnt" : "_perfect";
        int ext = baseOutputPath.LastIndexOf(".tres", StringComparison.OrdinalIgnoreCase);
        if (ext < 0) return baseOutputPath + suffix;
        return baseOutputPath[..ext] + suffix + ".tres";
    }

    /// <summary>Returns a display label for the quality tier.</summary>
    public static string QualityLabel(CookingQuality quality) => quality switch
    {
        CookingQuality.Burnt   => "Burnt!",
        CookingQuality.Normal  => "Normal",
        CookingQuality.Perfect => "Perfect!",
        _ => "???",
    };

    private static Dictionary<string, int> CountPaths(IEnumerable<string> paths)
    {
        var counts = new Dictionary<string, int>();
        foreach (var p in paths)
            counts[p] = counts.GetValueOrDefault(p) + 1;
        return counts;
    }
}
