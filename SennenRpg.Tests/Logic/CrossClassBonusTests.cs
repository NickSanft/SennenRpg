using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Linq;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class CrossClassBonusTests
{
    [Test]
    public void Registry_HasEntries()
    {
        Assert.That(CrossClassBonusRegistry.All, Is.Not.Empty);
    }

    [Test]
    public void Registry_NoDuplicateClassLevelPairs()
    {
        var pairs = CrossClassBonusRegistry.All
            .Select(b => (b.SourceClass, b.RequiredLevel))
            .ToList();

        Assert.That(pairs.Distinct().Count(), Is.EqualTo(pairs.Count),
            "Duplicate (SourceClass, RequiredLevel) pair found in registry");
    }

    [Test]
    public void Registry_AllRequiredLevelsPositive()
    {
        Assert.That(CrossClassBonusRegistry.All,
            Has.All.Matches<CrossClassBonus>(b => b.RequiredLevel > 0));
    }

    [Test]
    public void Registry_AllDescriptionsNonEmpty()
    {
        Assert.That(CrossClassBonusRegistry.All,
            Has.All.Matches<CrossClassBonus>(b => !string.IsNullOrWhiteSpace(b.Description)));
    }

    [Test]
    public void Registry_AllSixClassesRepresented()
    {
        var classes = CrossClassBonusRegistry.All
            .Select(b => b.SourceClass)
            .Distinct()
            .ToList();

        Assert.That(classes, Does.Contain(PlayerClass.Bard));
        Assert.That(classes, Does.Contain(PlayerClass.Fighter));
        Assert.That(classes, Does.Contain(PlayerClass.Ranger));
        Assert.That(classes, Does.Contain(PlayerClass.Mage));
        Assert.That(classes, Does.Contain(PlayerClass.Rogue));
        Assert.That(classes, Does.Contain(PlayerClass.Alchemist));
    }

    [Test]
    public void Registry_EachBonusHasStatSpellOrTag()
    {
        foreach (var bonus in CrossClassBonusRegistry.All)
        {
            bool hasStat  = bonus.StatBonuses != default;
            bool hasSpell = bonus.UnlockedSpellPath != null;
            bool hasTag   = !string.IsNullOrEmpty(bonus.Tag);
            Assert.That(hasStat || hasSpell || hasTag, Is.True,
                $"Bonus '{bonus.Description}' has neither stat bonuses, a spell unlock, nor a system tag");
        }
    }

    // ── Forager's Eye (Ranger Lv5) ────────────────────────────────────

    [Test]
    public void Registry_ForagersEye_RangerLv5_Exists()
    {
        var matches = CrossClassBonusRegistry.All
            .Where(b => b.Tag == CrossClassBonus.ForagersEye)
            .ToList();

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0].SourceClass,   Is.EqualTo(PlayerClass.Ranger));
        Assert.That(matches[0].RequiredLevel, Is.EqualTo(5));
    }

    [Test]
    public void HasTag_ReturnsFalse_BeforeRangerLv5()
    {
        var levels = new System.Collections.Generic.Dictionary<PlayerClass, int>
        {
            [PlayerClass.Ranger] = 4,
        };
        Assert.That(MultiClassLogic.HasTag(levels, CrossClassBonus.ForagersEye), Is.False);
    }

    [Test]
    public void HasTag_ReturnsTrue_AtRangerLv5()
    {
        var levels = new System.Collections.Generic.Dictionary<PlayerClass, int>
        {
            [PlayerClass.Ranger] = 5,
        };
        Assert.That(MultiClassLogic.HasTag(levels, CrossClassBonus.ForagersEye), Is.True);
    }

    [Test]
    public void HasTag_UnknownTag_ReturnsFalse()
    {
        var levels = new System.Collections.Generic.Dictionary<PlayerClass, int>
        {
            [PlayerClass.Ranger] = 99,
        };
        Assert.That(MultiClassLogic.HasTag(levels, "no_such_tag"), Is.False);
    }

    // ── Lucky Forager (Rogue Lv5) ─────────────────────────────────────

    [Test]
    public void Registry_LuckyForager_RogueLv5_Exists()
    {
        var matches = CrossClassBonusRegistry.All
            .Where(b => b.Tag == CrossClassBonus.LuckyForager)
            .ToList();

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches[0].SourceClass,   Is.EqualTo(PlayerClass.Rogue));
        Assert.That(matches[0].RequiredLevel, Is.EqualTo(5));
    }

    [Test]
    public void HasTag_LuckyForager_True_AtRogueLv5()
    {
        var levels = new System.Collections.Generic.Dictionary<PlayerClass, int>
        {
            [PlayerClass.Rogue] = 5,
        };
        Assert.That(MultiClassLogic.HasTag(levels, CrossClassBonus.LuckyForager), Is.True);
    }

    // ── Backstab spell (Rogue Lv10) ───────────────────────────────────

    [Test]
    public void GetCrossClassSpells_UnlocksBackstab_AtRogueLv10()
    {
        var levels = new System.Collections.Generic.Dictionary<PlayerClass, int>
        {
            [PlayerClass.Rogue] = 10,
        };
        var spells = MultiClassLogic.GetCrossClassSpells(levels);
        Assert.That(spells, Does.Contain("res://resources/spells/backstab.tres"));
    }

    // ── Alchemist tags ────────────────────────────────────────────────

    [Test]
    public void HasTag_WealthAura_True_AtAlchemistLv5()
    {
        var levels = new System.Collections.Generic.Dictionary<PlayerClass, int>
        {
            [PlayerClass.Alchemist] = 5,
        };
        Assert.That(MultiClassLogic.HasTag(levels, CrossClassBonus.WealthAura), Is.True);
    }

    [Test]
    public void HasTag_MasterBrewer_True_AtAlchemistLv10()
    {
        var levels = new System.Collections.Generic.Dictionary<PlayerClass, int>
        {
            [PlayerClass.Alchemist] = 10,
        };
        Assert.That(MultiClassLogic.HasTag(levels, CrossClassBonus.MasterBrewer), Is.True);
    }

    [Test]
    public void HasTag_MasterBrewer_False_AtAlchemistLv5()
    {
        var levels = new System.Collections.Generic.Dictionary<PlayerClass, int>
        {
            [PlayerClass.Alchemist] = 5,
        };
        Assert.That(MultiClassLogic.HasTag(levels, CrossClassBonus.MasterBrewer), Is.False);
    }
}
