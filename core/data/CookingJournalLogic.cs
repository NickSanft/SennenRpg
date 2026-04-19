using System;
using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure static logic for the Cooking Journal — tracks best quality achieved per recipe.
/// No Godot runtime dependency; fully NUnit-testable.
/// </summary>
public static class CookingJournalLogic
{
    /// <summary>
    /// Record a cooking attempt. Adds or upgrades the journal entry for the given recipe.
    /// Never downgrades: Perfect > Normal > Burnt.
    /// Returns the updated dictionary and whether the quality improved.
    /// </summary>
    public static (Dictionary<string, string> updated, bool improved) RecordAttempt(
        Dictionary<string, string> journal, string recipeId, CookingQuality quality)
    {
        journal ??= new Dictionary<string, string>();
        string qualityStr = quality.ToString();

        if (journal.TryGetValue(recipeId, out string? stored))
        {
            CookingQuality? storedQ = ParseQuality(stored);
            if (IsBetterQuality(quality, storedQ))
            {
                journal[recipeId] = qualityStr;
                return (journal, true);
            }
            return (journal, false);
        }

        journal[recipeId] = qualityStr;
        return (journal, true);
    }

    /// <summary>
    /// Returns true if <paramref name="newQ"/> is strictly better than <paramref name="storedQ"/>.
    /// A null stored quality (undiscovered) means any quality is an improvement.
    /// </summary>
    public static bool IsBetterQuality(CookingQuality newQ, CookingQuality? storedQ)
    {
        if (storedQ == null) return true;
        return (int)newQ > (int)storedQ.Value;
    }

    /// <summary>
    /// Count journal entries by quality tier.
    /// </summary>
    public static (int burnt, int normal, int perfect) CountByQuality(Dictionary<string, string> journal)
    {
        int burnt = 0, normal = 0, perfect = 0;
        if (journal == null) return (burnt, normal, perfect);

        foreach (var kvp in journal)
        {
            var q = ParseQuality(kvp.Value);
            switch (q)
            {
                case CookingQuality.Burnt:   burnt++;   break;
                case CookingQuality.Normal:  normal++;  break;
                case CookingQuality.Perfect: perfect++; break;
            }
        }
        return (burnt, normal, perfect);
    }

    /// <summary>
    /// Get display info for a recipe entry.
    /// Undiscovered recipes show "???" and an empty badge.
    /// </summary>
    public static (string displayName, string qualityBadge, bool discovered) GetDisplayInfo(
        string recipeId, string? displayName, Dictionary<string, string> journal)
    {
        if (journal == null || !journal.TryGetValue(recipeId, out string? stored))
            return ("???", "", false);

        string name = string.IsNullOrEmpty(displayName) ? recipeId : displayName;
        var quality = ParseQuality(stored);
        string badge = quality switch
        {
            CookingQuality.Burnt   => "[Burnt]",
            CookingQuality.Normal  => "[Normal]",
            CookingQuality.Perfect => "[Perfect]",
            _                      => "",
        };
        return (name, badge, true);
    }

    private static CookingQuality? ParseQuality(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (Enum.TryParse<CookingQuality>(value, out var q)) return q;
        return null;
    }
}
