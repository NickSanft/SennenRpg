using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Collections.Generic;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class MultiClassLogicTests
{
    [Test]
    public void GetEarnedBonuses_NoLevels_ReturnsEmpty()
    {
        var levels = new Dictionary<PlayerClass, int>();
        var earned = MultiClassLogic.GetEarnedBonuses(levels);

        Assert.That(earned, Is.Empty);
    }

    [Test]
    public void GetEarnedBonuses_FighterLevel5_ReturnsFighterLv5Bonus()
    {
        var levels = new Dictionary<PlayerClass, int>
        {
            { PlayerClass.Fighter, 5 },
        };

        var earned = MultiClassLogic.GetEarnedBonuses(levels);

        Assert.That(earned, Has.Count.EqualTo(1));
        Assert.That(earned[0].SourceClass, Is.EqualTo(PlayerClass.Fighter));
        Assert.That(earned[0].RequiredLevel, Is.EqualTo(5));
        Assert.That(earned[0].StatBonuses.Attack, Is.EqualTo(5));
    }

    [Test]
    public void GetEarnedBonuses_FighterLevel10_ReturnsBothFighterBonuses()
    {
        var levels = new Dictionary<PlayerClass, int>
        {
            { PlayerClass.Fighter, 10 },
        };

        var earned = MultiClassLogic.GetEarnedBonuses(levels);

        Assert.That(earned, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetEarnedBonuses_FighterLevel4_ReturnsNone()
    {
        var levels = new Dictionary<PlayerClass, int>
        {
            { PlayerClass.Fighter, 4 },
        };

        var earned = MultiClassLogic.GetEarnedBonuses(levels);

        Assert.That(earned, Is.Empty);
    }

    [Test]
    public void SumCrossClassBonuses_MultipleClasses_SumsCorrectly()
    {
        var levels = new Dictionary<PlayerClass, int>
        {
            { PlayerClass.Fighter, 5 },  // +5 ATK
            { PlayerClass.Ranger, 5 },   // +3 SPD
            { PlayerClass.Mage, 5 },     // +5 MAG
        };

        var bonus = MultiClassLogic.SumCrossClassBonuses(levels);

        Assert.That(bonus.Attack, Is.EqualTo(5));
        Assert.That(bonus.Speed, Is.EqualTo(3));
        Assert.That(bonus.Magic, Is.EqualTo(5));
        Assert.That(bonus.Defense, Is.EqualTo(0));
    }

    [Test]
    public void GetCrossClassSpells_BardLevel5_ReturnsShadowBolt()
    {
        var levels = new Dictionary<PlayerClass, int>
        {
            { PlayerClass.Bard, 5 },
        };

        var spells = MultiClassLogic.GetCrossClassSpells(levels);

        Assert.That(spells, Has.Count.EqualTo(1));
        Assert.That(spells[0], Does.Contain("shadow_bolt"));
    }

    [Test]
    public void GetCrossClassSpells_BardLevel4_ReturnsEmpty()
    {
        var levels = new Dictionary<PlayerClass, int>
        {
            { PlayerClass.Bard, 4 },
        };

        var spells = MultiClassLogic.GetCrossClassSpells(levels);

        Assert.That(spells, Is.Empty);
    }

    [Test]
    public void SnapshotToEntry_CapturesAllFields()
    {
        var entry = MultiClassLogic.SnapshotToEntry(
            PlayerClass.Fighter, level: 7, exp: 980,
            maxHp: 35, attack: 18, defense: 12, speed: 8,
            magic: 3, resistance: 5, luck: 6, maxMp: 0);

        Assert.That(entry.Class, Is.EqualTo(PlayerClass.Fighter));
        Assert.That(entry.Level, Is.EqualTo(7));
        Assert.That(entry.Exp, Is.EqualTo(980));
        Assert.That(entry.MaxHp, Is.EqualTo(35));
        Assert.That(entry.Attack, Is.EqualTo(18));
        Assert.That(entry.Defense, Is.EqualTo(12));
        Assert.That(entry.Speed, Is.EqualTo(8));
        Assert.That(entry.Magic, Is.EqualTo(3));
        Assert.That(entry.Resistance, Is.EqualTo(5));
        Assert.That(entry.Luck, Is.EqualTo(6));
        Assert.That(entry.MaxMp, Is.EqualTo(0));
    }
}
