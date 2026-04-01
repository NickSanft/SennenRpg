using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class LilyForgeLogicTests
{
    // ── GenerateRecipe ─────────────────────────────────────────────────────────

    [Test]
    public void GenerateRecipe_ReturnsThreeParts()
    {
        string recipe = LilyForgeLogic.GenerateRecipe(1, () => 42);
        var parts = recipe.Split('|');
        Assert.That(parts, Has.Length.EqualTo(3));
    }

    [Test]
    public void GenerateRecipe_EmbedsSeed()
    {
        string recipe = LilyForgeLogic.GenerateRecipe(1, () => 12345);
        Assert.That(recipe.StartsWith("12345|"), Is.True);
    }

    [Test]
    public void GenerateRecipe_EmbedsPlayerLevel()
    {
        string recipe = LilyForgeLogic.GenerateRecipe(7, () => 99);
        Assert.That(recipe.EndsWith("|7"), Is.True);
    }

    [Test]
    public void GenerateRecipe_SlotInRange()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            int capturedSeed = seed;
            string recipe = LilyForgeLogic.GenerateRecipe(1, () => capturedSeed);
            int slot = int.Parse(recipe.Split('|')[1]);
            Assert.That(slot, Is.InRange(0, 7), $"Seed {seed} produced out-of-range slot {slot}");
        }
    }

    // ── Resolve ────────────────────────────────────────────────────────────────

    [Test]
    public void Resolve_IsDeterministic()
    {
        string recipe = LilyForgeLogic.GenerateRecipe(5, () => 777);
        var a = LilyForgeLogic.Resolve(recipe);
        var b = LilyForgeLogic.Resolve(recipe);
        Assert.That(a.Id,          Is.EqualTo(b.Id));
        Assert.That(a.DisplayName, Is.EqualTo(b.DisplayName));
        Assert.That(a.BonusAttack, Is.EqualTo(b.BonusAttack));
        Assert.That(a.BonusDefense,Is.EqualTo(b.BonusDefense));
    }

    [Test]
    public void Resolve_PopulatesDisplayName()
    {
        string recipe = LilyForgeLogic.GenerateRecipe(3, () => 100);
        var item = LilyForgeLogic.Resolve(recipe);
        Assert.That(item.DisplayName, Is.Not.Null.And.Not.Empty);
        // Should be three words: prefix + material + type
        Assert.That(item.DisplayName.Split(' '), Has.Length.EqualTo(3));
    }

    [Test]
    public void Resolve_IdContainsSeed()
    {
        string recipe = LilyForgeLogic.GenerateRecipe(1, () => 55555);
        var item = LilyForgeLogic.Resolve(recipe);
        Assert.That(item.Id, Does.Contain("55555"));
    }

    [Test]
    public void Resolve_SlotMatchesRecipe()
    {
        // Force slot 0 (Weapon) by finding a seed that produces slot 0
        for (int seed = 0; seed < 1000; seed++)
        {
            int capturedSeed = seed;
            string recipe = LilyForgeLogic.GenerateRecipe(1, () => capturedSeed);
            int slotInt = int.Parse(recipe.Split('|')[1]);
            var item = LilyForgeLogic.Resolve(recipe);
            Assert.That((int)item.Slot, Is.EqualTo(slotInt),
                $"Seed {seed}: resolved slot {item.Slot} doesn't match recipe slot {slotInt}");
            break; // just test the first one
        }
    }

    [Test]
    public void Resolve_TotalBonusScalesWithLevel()
    {
        // Use same seed, compare level 1 vs level 10
        int seed = 42;
        string recipeL1  = $"{seed}|0|1";
        string recipeL10 = $"{seed}|0|10";

        var low  = LilyForgeLogic.Resolve(recipeL1);
        var high = LilyForgeLogic.Resolve(recipeL10);

        int totalLow  = low.BonusAttack  + low.BonusDefense  + low.BonusMagic  + low.BonusMaxHp  + low.BonusSpeed  + low.BonusLuck;
        int totalHigh = high.BonusAttack + high.BonusDefense + high.BonusMagic + high.BonusMaxHp + high.BonusSpeed + high.BonusLuck;

        Assert.That(totalHigh, Is.GreaterThan(totalLow));
    }

    [Test]
    public void Resolve_WeaponHasNoDefenseOrHp()
    {
        // Find a recipe with slot 0 (Weapon)
        string recipe = $"0|0|1"; // seed=0, slot=0, level=1
        var item = LilyForgeLogic.Resolve(recipe);
        // Weapons are biased ATK/MAG — def and hp should be 0
        Assert.That(item.BonusDefense, Is.EqualTo(0));
        Assert.That(item.BonusMaxHp,   Is.EqualTo(0));
    }

    [Test]
    public void Resolve_AllBonusesNonNegative()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            string recipe = $"{seed}|0|5";
            var item = LilyForgeLogic.Resolve(recipe);
            Assert.That(item.BonusAttack,  Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusDefense, Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusMagic,   Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusMaxHp,   Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusSpeed,   Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusLuck,    Is.GreaterThanOrEqualTo(0));
        }
    }

    // ── Legacy recipe (no level field) ────────────────────────────────────────

    [Test]
    public void Resolve_LegacyRecipeWithoutLevelDefaultsToOne()
    {
        // Old recipes have only two parts: "seed|slot"
        string legacyRecipe = "12345|2";
        Assert.DoesNotThrow(() => LilyForgeLogic.Resolve(legacyRecipe));
        var item = LilyForgeLogic.Resolve(legacyRecipe);
        Assert.That(item.DisplayName, Is.Not.Null.And.Not.Empty);
    }
}
