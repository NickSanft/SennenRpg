using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class TrophyTests
{
    // ── Registry validation ─────────────────────────────────────────────

    [Test]
    public void Registry_NoDuplicateIds()
    {
        var ids = TrophyRegistry.All.Select(t => t.Id).ToList();
        Assert.That(ids, Is.Unique);
    }

    [Test]
    public void Registry_AllDescriptionsNonEmpty()
    {
        foreach (var t in TrophyRegistry.All)
            Assert.That(t.Description, Is.Not.Null.And.Not.Empty, $"Trophy {t.Id} has empty description");
    }

    [Test]
    public void Registry_AllIconLettersNonEmpty()
    {
        foreach (var t in TrophyRegistry.All)
            Assert.That(t.IconLetter, Is.Not.Null.And.Not.Empty, $"Trophy {t.Id} has empty icon letter");
    }

    [Test]
    public void Registry_AllDisplayNamesNonEmpty()
    {
        foreach (var t in TrophyRegistry.All)
            Assert.That(t.DisplayName, Is.Not.Null.And.Not.Empty, $"Trophy {t.Id} has empty display name");
    }

    [Test]
    public void Registry_AllCategoriesValid()
    {
        foreach (var t in TrophyRegistry.All)
            Assert.That(System.Enum.IsDefined(t.Category), Is.True, $"Trophy {t.Id} has invalid category");
    }

    [Test]
    public void Registry_FindReturnsCorrectTrophy()
    {
        var trophy = TrophyRegistry.Find("first_blood");
        Assert.That(trophy, Is.Not.Null);
        Assert.That(trophy!.Value.DisplayName, Is.EqualTo("First Blood"));
    }

    [Test]
    public void Registry_FindReturnsNullForUnknown()
    {
        Assert.That(TrophyRegistry.Find("nonexistent"), Is.Null);
    }

    // ── CheckCondition ──────────────────────────────────────────────────

    [Test]
    public void CheckCondition_EmptyData_ReturnsFalseForAll()
    {
        var data = new TrophyCheckData();
        foreach (var t in TrophyRegistry.All)
        {
            bool result = TrophyLogic.CheckCondition(t.Id, data);
            Assert.That(result, Is.False, $"Trophy {t.Id} should not unlock with empty data");
        }
    }

    [Test]
    public void CheckCondition_FirstBlood_OneKill()
    {
        var data = new TrophyCheckData { TotalKills = 1 };
        Assert.That(TrophyLogic.CheckCondition("first_blood", data), Is.True);
    }

    [Test]
    public void CheckCondition_Centurion_Requires100Kills()
    {
        Assert.That(TrophyLogic.CheckCondition("centurion", new TrophyCheckData { TotalKills = 99 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("centurion", new TrophyCheckData { TotalKills = 100 }), Is.True);
    }

    [Test]
    public void CheckCondition_FlawlessRhythm_RequiresSRank()
    {
        Assert.That(TrophyLogic.CheckCondition("flawless_rhythm", new TrophyCheckData { HasSRank = false }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("flawless_rhythm", new TrophyCheckData { HasSRank = true }), Is.True);
    }

    [Test]
    public void CheckCondition_ComboMaster_Requires30Streak()
    {
        Assert.That(TrophyLogic.CheckCondition("combo_master", new TrophyCheckData { MaxComboStreak = 29 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("combo_master", new TrophyCheckData { MaxComboStreak = 30 }), Is.True);
    }

    [Test]
    public void CheckCondition_FirstCourse_OneMeal()
    {
        var data = new TrophyCheckData { TotalMealsCooked = 1 };
        Assert.That(TrophyLogic.CheckCondition("first_course", data), Is.True);
    }

    [Test]
    public void CheckCondition_MasterChef_Requires10PerfectMeals()
    {
        Assert.That(TrophyLogic.CheckCondition("master_chef", new TrophyCheckData { PerfectMeals = 9 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("master_chef", new TrophyCheckData { PerfectMeals = 10 }), Is.True);
    }

    [Test]
    public void CheckCondition_RecipeCollector_AllRecipes()
    {
        Assert.That(TrophyLogic.CheckCondition("recipe_collector",
            new TrophyCheckData { TotalRecipes = 4, TotalRecipeCount = 5 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("recipe_collector",
            new TrophyCheckData { TotalRecipes = 5, TotalRecipeCount = 5 }), Is.True);
    }

    [Test]
    public void CheckCondition_RecipeCollector_ZeroTotal_ReturnsFalse()
    {
        Assert.That(TrophyLogic.CheckCondition("recipe_collector",
            new TrophyCheckData { TotalRecipes = 0, TotalRecipeCount = 0 }), Is.False);
    }

    [Test]
    public void CheckCondition_DungeonDiver_ReachedFloor3()
    {
        Assert.That(TrophyLogic.CheckCondition("dungeon_diver", new TrophyCheckData { ReachedFloor3 = false }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("dungeon_diver", new TrophyCheckData { ReachedFloor3 = true }), Is.True);
    }

    [Test]
    public void CheckCondition_NightOwl_10NightBattles()
    {
        Assert.That(TrophyLogic.CheckCondition("night_owl", new TrophyCheckData { NightBattles = 9 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("night_owl", new TrophyCheckData { NightBattles = 10 }), Is.True);
    }

    [Test]
    public void CheckCondition_Hoarder_50Items()
    {
        Assert.That(TrophyLogic.CheckCondition("hoarder", new TrophyCheckData { ItemCount = 49 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("hoarder", new TrophyCheckData { ItemCount = 50 }), Is.True);
    }

    [Test]
    public void CheckCondition_JunkDealer_500GoldSold()
    {
        Assert.That(TrophyLogic.CheckCondition("junk_dealer", new TrophyCheckData { JunkGoldSold = 499 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("junk_dealer", new TrophyCheckData { JunkGoldSold = 500 }), Is.True);
    }

    [Test]
    public void CheckCondition_MultiTalented_3ClassesAtLevel5()
    {
        Assert.That(TrophyLogic.CheckCondition("multi_talented", new TrophyCheckData { ClassesAtLevel5 = 2 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("multi_talented", new TrophyCheckData { ClassesAtLevel5 = 3 }), Is.True);
    }

    [Test]
    public void CheckCondition_ClassMaster_Level20()
    {
        Assert.That(TrophyLogic.CheckCondition("class_master", new TrophyCheckData { MaxClassLevel = 19 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("class_master", new TrophyCheckData { MaxClassLevel = 20 }), Is.True);
    }

    [Test]
    public void CheckCondition_FullParty_AllRecruited()
    {
        Assert.That(TrophyLogic.CheckCondition("full_party",
            new TrophyCheckData { PartySize = 3, TotalPartyMembers = 4 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("full_party",
            new TrophyCheckData { PartySize = 4, TotalPartyMembers = 4 }), Is.True);
    }

    [Test]
    public void CheckCondition_Cartographer_AllMaps()
    {
        Assert.That(TrophyLogic.CheckCondition("cartographer",
            new TrophyCheckData { MapsVisited = 9, TotalMapCount = 10 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("cartographer",
            new TrophyCheckData { MapsVisited = 10, TotalMapCount = 10 }), Is.True);
    }

    [Test]
    public void CheckCondition_BestiaryScholar_AllEnemies()
    {
        Assert.That(TrophyLogic.CheckCondition("bestiary_scholar",
            new TrophyCheckData { DiscoveredEnemies = 7, TotalEnemyTypes = 8 }), Is.False);
        Assert.That(TrophyLogic.CheckCondition("bestiary_scholar",
            new TrophyCheckData { DiscoveredEnemies = 8, TotalEnemyTypes = 8 }), Is.True);
    }

    [Test]
    public void CheckCondition_UnknownId_ReturnsFalse()
    {
        Assert.That(TrophyLogic.CheckCondition("nonexistent", new TrophyCheckData()), Is.False);
    }

    // ── CheckAllNewUnlocks ──────────────────────────────────────────────

    [Test]
    public void CheckAllNewUnlocks_ExcludesAlreadyUnlocked()
    {
        var data = new TrophyCheckData { TotalKills = 1 };
        var already = new HashSet<string> { "first_blood" };
        var result = TrophyLogic.CheckAllNewUnlocks(data, already);
        Assert.That(result, Does.Not.Contain("first_blood"));
    }

    [Test]
    public void CheckAllNewUnlocks_ReturnsNewlyEligible()
    {
        var data = new TrophyCheckData { TotalKills = 1, TotalMealsCooked = 1 };
        var already = new HashSet<string>();
        var result = TrophyLogic.CheckAllNewUnlocks(data, already);
        Assert.That(result, Does.Contain("first_blood"));
        Assert.That(result, Does.Contain("first_course"));
    }

    [Test]
    public void CheckAllNewUnlocks_EmptyData_ReturnsEmpty()
    {
        var result = TrophyLogic.CheckAllNewUnlocks(new TrophyCheckData(), new HashSet<string>());
        Assert.That(result, Is.Empty);
    }

    // ── DisplayInfo ─────────────────────────────────────────────────────

    [Test]
    public void DisplayInfo_HiddenAndLocked_ReturnsQuestionMarks()
    {
        var hidden = TrophyRegistry.All.First(t => t.IsHidden);
        var (name, desc) = TrophyLogic.DisplayInfo(hidden, isUnlocked: false);
        Assert.That(name, Is.EqualTo("???"));
        Assert.That(desc, Is.EqualTo("???"));
    }

    [Test]
    public void DisplayInfo_HiddenAndUnlocked_ReturnsRealInfo()
    {
        var hidden = TrophyRegistry.All.First(t => t.IsHidden);
        var (name, desc) = TrophyLogic.DisplayInfo(hidden, isUnlocked: true);
        Assert.That(name, Is.EqualTo(hidden.DisplayName));
        Assert.That(desc, Is.EqualTo(hidden.Description));
    }

    [Test]
    public void DisplayInfo_NotHiddenAndLocked_ReturnsRealName()
    {
        var visible = TrophyRegistry.All.First(t => !t.IsHidden);
        var (name, desc) = TrophyLogic.DisplayInfo(visible, isUnlocked: false);
        Assert.That(name, Is.EqualTo(visible.DisplayName));
        Assert.That(desc, Is.EqualTo(visible.Description));
    }

    // ── CountByCategory ─────────────────────────────────────────────────

    [Test]
    public void CountByCategory_AllCategories_Present()
    {
        var counts = TrophyLogic.CountByCategory(new HashSet<string>());
        foreach (var cat in System.Enum.GetValues<TrophyCategory>())
            Assert.That(counts.ContainsKey(cat), Is.True);
    }

    [Test]
    public void CountByCategory_CorrectTotals()
    {
        var counts = TrophyLogic.CountByCategory(new HashSet<string>());
        int sumTotal = counts.Values.Sum(v => v.total);
        Assert.That(sumTotal, Is.EqualTo(TrophyRegistry.All.Length));
    }

    [Test]
    public void CountByCategory_UnlockedCountsCorrect()
    {
        var unlocked = new HashSet<string> { "first_blood", "centurion" };
        var counts = TrophyLogic.CountByCategory(unlocked);
        Assert.That(counts[TrophyCategory.Combat].unlocked, Is.EqualTo(2));
        Assert.That(counts[TrophyCategory.Cooking].unlocked, Is.EqualTo(0));
    }

    [Test]
    public void CountByCategory_CombatTotalMatchesRegistry()
    {
        int expected = TrophyRegistry.All.Count(t => t.Category == TrophyCategory.Combat);
        var counts = TrophyLogic.CountByCategory(new HashSet<string>());
        Assert.That(counts[TrophyCategory.Combat].total, Is.EqualTo(expected));
    }
}
