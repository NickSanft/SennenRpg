using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Collections.Generic;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class PartyMemberStatsLogicTests
{
    private static PartyMember Sample(string id = "sen") => new()
    {
        MemberId   = id,
        MaxHp      = 30, CurrentHp = 25,
        MaxMp      = 10, CurrentMp = 8,
        Attack     = 12,
        Defense    = 5,
        Speed      = 8,
        Magic      = 4,
        Resistance = 3,
        Luck       = 6,
    };

    [Test]
    public void ComputeEffective_NoBonuses_ReturnsBaseStats()
    {
        var sen = Sample();
        var stats = PartyMemberStatsLogic.ComputeEffective(sen, default(EquipmentBonuses));

        Assert.That(stats.MaxHp,      Is.EqualTo(30));
        Assert.That(stats.CurrentHp,  Is.EqualTo(25));
        Assert.That(stats.MaxMp,      Is.EqualTo(10));
        Assert.That(stats.CurrentMp,  Is.EqualTo(8));
        Assert.That(stats.Attack,     Is.EqualTo(12));
        Assert.That(stats.Defense,    Is.EqualTo(5));
        Assert.That(stats.Speed,      Is.EqualTo(8));
        Assert.That(stats.Magic,      Is.EqualTo(4));
        Assert.That(stats.Resistance, Is.EqualTo(3));
        Assert.That(stats.Luck,       Is.EqualTo(6));
    }

    [Test]
    public void ComputeEffective_WithBonuses_AddsToBaseStats()
    {
        var sen = Sample();
        var bonus = new EquipmentBonuses(MaxHp: 5, Attack: 3, Defense: 2, Speed: 1, Luck: 4);
        var stats = PartyMemberStatsLogic.ComputeEffective(sen, bonus);

        Assert.That(stats.MaxHp,   Is.EqualTo(35));
        Assert.That(stats.Attack,  Is.EqualTo(15));
        Assert.That(stats.Defense, Is.EqualTo(7));
        Assert.That(stats.Speed,   Is.EqualTo(9));
        Assert.That(stats.Luck,    Is.EqualTo(10));
        // Untouched stats remain at base.
        Assert.That(stats.Magic,      Is.EqualTo(4));
        Assert.That(stats.Resistance, Is.EqualTo(3));
        // Current HP/MP are *not* affected by equipment bonuses — they're live values.
        Assert.That(stats.CurrentHp, Is.EqualTo(25));
        Assert.That(stats.CurrentMp, Is.EqualTo(8));
    }

    [Test]
    public void ComputeEffective_FromList_SumsBonusesViaEquipmentLogic()
    {
        var sen = Sample();
        var helm   = new EquipmentBonuses(MaxHp:  6, Defense: 1);
        var sword  = new EquipmentBonuses(Attack: 4);
        var amulet = new EquipmentBonuses(Magic:  3, Luck: 2);

        var stats = PartyMemberStatsLogic.ComputeEffective(
            sen, new List<EquipmentBonuses> { helm, sword, amulet });

        Assert.That(stats.MaxHp,   Is.EqualTo(36));
        Assert.That(stats.Defense, Is.EqualTo(6));
        Assert.That(stats.Attack,  Is.EqualTo(16));
        Assert.That(stats.Magic,   Is.EqualTo(7));
        Assert.That(stats.Luck,    Is.EqualTo(8));
    }

    [Test]
    public void ComputeEffective_NullMember_ReturnsDefault()
    {
        var stats = PartyMemberStatsLogic.ComputeEffective(null!, new EquipmentBonuses(Attack: 99));
        Assert.That(stats, Is.EqualTo(default(PartyMemberEffectiveStats)));
    }

    [Test]
    public void TwoMembers_SeparateBonuses_NoCrossContamination()
    {
        var sen  = Sample("sen");
        var lily = Sample("lily");
        lily.Magic = 12; // Lily is the spellcaster

        var senBonus  = new EquipmentBonuses(Attack: 5);
        var lilyBonus = new EquipmentBonuses(Magic: 6);

        var senStats  = PartyMemberStatsLogic.ComputeEffective(sen,  senBonus);
        var lilyStats = PartyMemberStatsLogic.ComputeEffective(lily, lilyBonus);

        // Sen got the +5 ATK, Lily did not. Lily got the +6 MAG, Sen did not.
        Assert.That(senStats.Attack,  Is.EqualTo(17));
        Assert.That(lilyStats.Attack, Is.EqualTo(12));
        Assert.That(senStats.Magic,   Is.EqualTo(4));
        Assert.That(lilyStats.Magic,  Is.EqualTo(18));
    }
}
