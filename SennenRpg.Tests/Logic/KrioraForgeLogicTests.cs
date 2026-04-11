using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class KrioraForgeLogicTests
{
    // ── GenerateRecipe ─────────────────────────────────────────────────────────

    [Test]
    public void GenerateRecipe_ReturnsThreeParts()
    {
        string recipe = KrioraForgeLogic.GenerateRecipe(1, () => 42);
        var parts = recipe.Split('|');
        Assert.That(parts, Has.Length.EqualTo(3));
    }

    [Test]
    public void GenerateRecipe_EmbedsSeed()
    {
        string recipe = KrioraForgeLogic.GenerateRecipe(1, () => 12345);
        Assert.That(recipe.StartsWith("12345|"), Is.True);
    }

    [Test]
    public void GenerateRecipe_EmbedsPlayerLevel()
    {
        string recipe = KrioraForgeLogic.GenerateRecipe(7, () => 99);
        Assert.That(recipe.EndsWith("|7"), Is.True);
    }

    [Test]
    public void GenerateRecipe_SlotIsWeaponOrAccessory()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            int capturedSeed = seed;
            string recipe = KrioraForgeLogic.GenerateRecipe(1, () => capturedSeed);
            int slot = int.Parse(recipe.Split('|')[1]);
            Assert.That(slot, Is.AnyOf((int)EquipmentSlot.Weapon, (int)EquipmentSlot.Accessory),
                $"Seed {seed} produced unexpected slot {slot}");
        }
    }

    // ── Resolve ────────────────────────────────────────────────────────────────

    [Test]
    public void Resolve_IsDeterministic()
    {
        string recipe = KrioraForgeLogic.GenerateRecipe(5, () => 777);
        var a = KrioraForgeLogic.Resolve(recipe);
        var b = KrioraForgeLogic.Resolve(recipe);
        Assert.That(a.Id,          Is.EqualTo(b.Id));
        Assert.That(a.DisplayName, Is.EqualTo(b.DisplayName));
        Assert.That(a.BonusMagic,  Is.EqualTo(b.BonusMagic));
    }

    [Test]
    public void Resolve_IdContainsKrioraPrefix()
    {
        string recipe = KrioraForgeLogic.GenerateRecipe(1, () => 55555);
        var item = KrioraForgeLogic.Resolve(recipe);
        Assert.That(item.Id, Does.StartWith("kriora_"));
    }

    [Test]
    public void Resolve_PopulatesDisplayName()
    {
        string recipe = KrioraForgeLogic.GenerateRecipe(3, () => 100);
        var item = KrioraForgeLogic.Resolve(recipe);
        Assert.That(item.DisplayName, Is.Not.Null.And.Not.Empty);
        // Should be three words: prefix + material + type
        Assert.That(item.DisplayName.Split(' '), Has.Length.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void Resolve_WeaponIsMagicBiased()
    {
        // Generate many Weapon-slot items, verify Magic > Attack on average
        int totalMagic = 0, totalAttack = 0;
        for (int seed = 0; seed < 50; seed++)
        {
            string recipe = $"{seed}|{(int)EquipmentSlot.Weapon}|10";
            var item = KrioraForgeLogic.Resolve(recipe);
            totalMagic  += item.BonusMagic;
            totalAttack += item.BonusAttack;
        }
        Assert.That(totalMagic, Is.GreaterThan(totalAttack),
            "Crystal weapons should be heavily biased toward Magic.");
    }

    [Test]
    public void Resolve_TotalBonusScalesWithLevel()
    {
        int seed = 42;
        string recipeL1  = $"{seed}|{(int)EquipmentSlot.Weapon}|1";
        string recipeL10 = $"{seed}|{(int)EquipmentSlot.Weapon}|10";

        var low  = KrioraForgeLogic.Resolve(recipeL1);
        var high = KrioraForgeLogic.Resolve(recipeL10);

        int totalLow  = low.BonusAttack  + low.BonusDefense  + low.BonusMagic  + low.BonusMaxHp  + low.BonusSpeed  + low.BonusLuck;
        int totalHigh = high.BonusAttack + high.BonusDefense + high.BonusMagic + high.BonusMaxHp + high.BonusSpeed + high.BonusLuck;

        Assert.That(totalHigh, Is.GreaterThan(totalLow));
    }

    [Test]
    public void Resolve_AllBonusesNonNegative()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            string recipe = $"{seed}|{(int)EquipmentSlot.Weapon}|5";
            var item = KrioraForgeLogic.Resolve(recipe);
            Assert.That(item.BonusAttack,  Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusDefense, Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusMagic,   Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusMaxHp,   Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusSpeed,   Is.GreaterThanOrEqualTo(0));
            Assert.That(item.BonusLuck,    Is.GreaterThanOrEqualTo(0));
        }
    }

    [Test]
    public void Resolve_DescriptionMentionsKriora()
    {
        string recipe = KrioraForgeLogic.GenerateRecipe(1, () => 1);
        var item = KrioraForgeLogic.Resolve(recipe);
        Assert.That(item.Description, Does.Contain("Kriora"));
    }
}
