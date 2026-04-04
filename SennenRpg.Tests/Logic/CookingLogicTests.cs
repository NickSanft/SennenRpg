using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class CookingLogicTests
{
    private const string Meat  = "res://resources/items/ingredient_mystery_meat.tres";
    private const string Bread = "res://resources/items/ingredient_bread.tres";
    private const string Ecto  = "res://resources/items/ingredient_ecto_essence.tres";

    // ── HasIngredients ────────────────────────────────────────────────

    [Test]
    public void HasIngredients_AllPresent_ReturnsTrue()
    {
        string[] inv = [Meat, Bread, Ecto];
        RecipeIngredient[] recipe = [new(Meat, 1), new(Bread, 1)];

        Assert.That(CookingLogic.HasIngredients(inv, recipe), Is.True);
    }

    [Test]
    public void HasIngredients_MissingOne_ReturnsFalse()
    {
        string[] inv = [Meat];
        RecipeIngredient[] recipe = [new(Meat, 1), new(Bread, 1)];

        Assert.That(CookingLogic.HasIngredients(inv, recipe), Is.False);
    }

    [Test]
    public void HasIngredients_NeedsTwoOfSame_HasTwo_ReturnsTrue()
    {
        string[] inv = [Meat, Meat, Bread];
        RecipeIngredient[] recipe = [new(Meat, 2)];

        Assert.That(CookingLogic.HasIngredients(inv, recipe), Is.True);
    }

    [Test]
    public void HasIngredients_NeedsTwoOfSame_HasOne_ReturnsFalse()
    {
        string[] inv = [Meat, Bread];
        RecipeIngredient[] recipe = [new(Meat, 2)];

        Assert.That(CookingLogic.HasIngredients(inv, recipe), Is.False);
    }

    [Test]
    public void HasIngredients_EmptyInventory_ReturnsFalse()
    {
        RecipeIngredient[] recipe = [new(Meat, 1)];

        Assert.That(CookingLogic.HasIngredients([], recipe), Is.False);
    }

    [Test]
    public void HasIngredients_EmptyRecipe_ReturnsTrue()
    {
        string[] inv = [Meat];

        Assert.That(CookingLogic.HasIngredients(inv, []), Is.True);
    }

    // ── ConsumeIngredients ────────────────────────────────────────────

    [Test]
    public void ConsumeIngredients_RemovesCorrectItems_LeavesRest()
    {
        string[] inv = [Meat, Bread, Ecto];
        RecipeIngredient[] recipe = [new(Meat, 1), new(Bread, 1)];

        var remaining = CookingLogic.ConsumeIngredients(inv, recipe);

        Assert.That(remaining, Is.Not.Null);
        Assert.That(remaining, Has.Count.EqualTo(1));
        Assert.That(remaining![0], Is.EqualTo(Ecto));
    }

    [Test]
    public void ConsumeIngredients_InsufficientStock_ReturnsNull()
    {
        string[] inv = [Meat];
        RecipeIngredient[] recipe = [new(Meat, 1), new(Bread, 1)];

        Assert.That(CookingLogic.ConsumeIngredients(inv, recipe), Is.Null);
    }

    [Test]
    public void ConsumeIngredients_RemovesTwoOfSame()
    {
        string[] inv = [Meat, Meat, Meat, Bread];
        RecipeIngredient[] recipe = [new(Meat, 2)];

        var remaining = CookingLogic.ConsumeIngredients(inv, recipe);

        Assert.That(remaining, Is.Not.Null);
        Assert.That(remaining!.Count(p => p == Meat), Is.EqualTo(1));
        Assert.That(remaining.Count(p => p == Bread), Is.EqualTo(1));
    }

    // ── DetermineQuality ──────────────────────────────────────────────

    [Test]
    public void DetermineQuality_AllPerfect_ReturnsPerfect()
    {
        Assert.That(CookingLogic.DetermineQuality(6, 0, 6), Is.EqualTo(CookingQuality.Perfect));
    }

    [Test]
    public void DetermineQuality_AllMiss_ReturnsBurnt()
    {
        Assert.That(CookingLogic.DetermineQuality(0, 0, 6), Is.EqualTo(CookingQuality.Burnt));
    }

    [Test]
    public void DetermineQuality_MixedAboveHalfHit_ReturnsNormal()
    {
        // 4/6 hit (67%), but only 1 perfect out of 4 hits (25% < 50%)
        Assert.That(CookingLogic.DetermineQuality(1, 3, 6), Is.EqualTo(CookingQuality.Normal));
    }

    [Test]
    public void DetermineQuality_HighHitHighPerfect_ReturnsPerfect()
    {
        // 5/6 hit (83%), 3 perfect out of 5 hits (60% >= 50%)
        Assert.That(CookingLogic.DetermineQuality(3, 2, 6), Is.EqualTo(CookingQuality.Perfect));
    }

    [Test]
    public void DetermineQuality_BelowHalfHit_ReturnsBurnt()
    {
        // 2/6 hit (33%)
        Assert.That(CookingLogic.DetermineQuality(1, 1, 6), Is.EqualTo(CookingQuality.Burnt));
    }

    [Test]
    public void DetermineQuality_ZeroNotes_ReturnsBurnt()
    {
        Assert.That(CookingLogic.DetermineQuality(0, 0, 0), Is.EqualTo(CookingQuality.Burnt));
    }

    [Test]
    public void DetermineQuality_ExactlyHalf_ReturnsNormal()
    {
        // 3/6 hit (50%)
        Assert.That(CookingLogic.DetermineQuality(0, 3, 6), Is.EqualTo(CookingQuality.Normal));
    }

    // ── QualityHealBonus ──────────────────────────────────────────────

    [Test]
    public void QualityHealBonus_Burnt_HalvesHeal()
    {
        Assert.That(CookingLogic.QualityHealBonus(30, CookingQuality.Burnt), Is.EqualTo(15));
    }

    [Test]
    public void QualityHealBonus_Normal_ReturnsBase()
    {
        Assert.That(CookingLogic.QualityHealBonus(30, CookingQuality.Normal), Is.EqualTo(30));
    }

    [Test]
    public void QualityHealBonus_Perfect_Returns150Percent()
    {
        Assert.That(CookingLogic.QualityHealBonus(30, CookingQuality.Perfect), Is.EqualTo(45));
    }

    // ── QualityItemPath ───────────────────────────────────────────────

    [Test]
    public void QualityItemPath_Normal_ReturnsUnchanged()
    {
        const string path = "res://resources/items/cooked_foo.tres";
        Assert.That(CookingLogic.QualityItemPath(path, CookingQuality.Normal), Is.EqualTo(path));
    }

    [Test]
    public void QualityItemPath_Burnt_InsertsSuffix()
    {
        Assert.That(
            CookingLogic.QualityItemPath("res://resources/items/cooked_foo.tres", CookingQuality.Burnt),
            Is.EqualTo("res://resources/items/cooked_foo_burnt.tres"));
    }

    [Test]
    public void QualityItemPath_Perfect_InsertsSuffix()
    {
        Assert.That(
            CookingLogic.QualityItemPath("res://resources/items/cooked_foo.tres", CookingQuality.Perfect),
            Is.EqualTo("res://resources/items/cooked_foo_perfect.tres"));
    }
}
