using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure static logic for the trophy/achievement system.
/// No Godot dependencies — fully NUnit-testable.
/// </summary>
public static class TrophyLogic
{
    /// <summary>
    /// Check whether a specific trophy's condition is met.
    /// </summary>
    public static bool CheckCondition(string trophyId, TrophyCheckData data)
    {
        return trophyId switch
        {
            // Combat
            "first_blood"         => data.TotalKills >= 1,
            "centurion"           => data.TotalKills >= 100,
            "flawless_rhythm"     => data.HasSRank,
            "combo_master"        => data.MaxComboStreak >= 30,
            "party_wipe_survivor" => false, // checked at battle-end, not from snapshot
            // Cooking
            "first_course"        => data.TotalMealsCooked >= 1,
            "master_chef"         => data.PerfectMeals >= 10,
            "recipe_collector"    => data.TotalRecipeCount > 0 && data.TotalRecipes >= data.TotalRecipeCount,
            // Exploration
            "cartographer"        => data.TotalMapCount > 0 && data.MapsVisited >= data.TotalMapCount,
            "dungeon_diver"       => data.ReachedFloor3,
            "night_owl"           => data.NightBattles >= 10,
            // Collection
            "hoarder"             => data.ItemCount >= 50,
            "bestiary_scholar"    => data.TotalEnemyTypes > 0 && data.DiscoveredEnemies >= data.TotalEnemyTypes,
            "junk_dealer"         => data.JunkGoldSold >= 500,
            // Mastery
            "multi_talented"      => data.ClassesAtLevel5 >= 3,
            "class_master"        => data.MaxClassLevel >= 20,
            "full_party"          => data.TotalPartyMembers > 0 && data.PartySize >= data.TotalPartyMembers,
            _                     => false,
        };
    }

    /// <summary>
    /// Returns the IDs of all trophies that are newly unlocked (condition met
    /// and not already in <paramref name="alreadyUnlocked"/>).
    /// </summary>
    public static List<string> CheckAllNewUnlocks(TrophyCheckData data, IReadOnlySet<string> alreadyUnlocked)
    {
        var result = new List<string>();
        foreach (var trophy in TrophyRegistry.All)
        {
            if (alreadyUnlocked.Contains(trophy.Id))
                continue;
            if (CheckCondition(trophy.Id, data))
                result.Add(trophy.Id);
        }
        return result;
    }

    /// <summary>
    /// Returns display-safe name and description. Hidden trophies that are
    /// still locked show "???" for both fields.
    /// </summary>
    public static (string name, string desc) DisplayInfo(Trophy trophy, bool isUnlocked)
    {
        if (trophy.IsHidden && !isUnlocked)
            return ("???", "???");
        return (trophy.DisplayName, trophy.Description);
    }

    /// <summary>
    /// Count unlocked vs total trophies per category.
    /// </summary>
    public static Dictionary<TrophyCategory, (int unlocked, int total)> CountByCategory(
        IReadOnlySet<string> unlockedIds)
    {
        var counts = new Dictionary<TrophyCategory, (int unlocked, int total)>();
        foreach (var cat in System.Enum.GetValues<TrophyCategory>())
            counts[cat] = (0, 0);

        foreach (var trophy in TrophyRegistry.All)
        {
            var (u, t) = counts[trophy.Category];
            t++;
            if (unlockedIds.Contains(trophy.Id))
                u++;
            counts[trophy.Category] = (u, t);
        }
        return counts;
    }
}
