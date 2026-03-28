using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class EquipmentLogicTest
{
    // ── SumBonuses ────────────────────────────────────────────────────────────

    [Test]
    public void SumBonuses_NoItems_ReturnsAllZeros()
    {
        var result = EquipmentLogic.SumBonuses([]);
        Assert.That(result, Is.EqualTo(new EquipmentBonuses()));
    }

    [Test]
    public void SumBonuses_SingleWeapon_AddsAttack()
    {
        var weapon = new EquipmentBonuses(Attack: 5);
        var result = EquipmentLogic.SumBonuses([weapon]);
        Assert.That(result.Attack, Is.EqualTo(5));
        Assert.That(result.Defense, Is.EqualTo(0));
        Assert.That(result.Luck, Is.EqualTo(0));
    }

    [Test]
    public void SumBonuses_MultipleItems_SumsAllFields()
    {
        var sword   = new EquipmentBonuses(Attack: 5);
        var armour  = new EquipmentBonuses(MaxHp: 5, Defense: 4);
        var cap     = new EquipmentBonuses(Defense: 2);
        var charm   = new EquipmentBonuses(Luck: 10);

        var result = EquipmentLogic.SumBonuses([sword, armour, cap, charm]);

        Assert.That(result.MaxHp,   Is.EqualTo(5));
        Assert.That(result.Attack,  Is.EqualTo(5));
        Assert.That(result.Defense, Is.EqualTo(6));
        Assert.That(result.Luck,    Is.EqualTo(10));
    }

    [Test]
    public void SumBonuses_AllStatsRespected()
    {
        var full = new EquipmentBonuses(
            MaxHp:      3,
            Attack:     4,
            Defense:    5,
            Magic:      6,
            Resistance: 7,
            Speed:      8,
            Luck:       9);

        var result = EquipmentLogic.SumBonuses([full]);

        Assert.Multiple(() =>
        {
            Assert.That(result.MaxHp,      Is.EqualTo(3));
            Assert.That(result.Attack,     Is.EqualTo(4));
            Assert.That(result.Defense,    Is.EqualTo(5));
            Assert.That(result.Magic,      Is.EqualTo(6));
            Assert.That(result.Resistance, Is.EqualTo(7));
            Assert.That(result.Speed,      Is.EqualTo(8));
            Assert.That(result.Luck,       Is.EqualTo(9));
        });
    }

    [Test]
    public void SumBonuses_TwoItemsSameSlot_BothApplied()
    {
        // Logic layer doesn't enforce one-per-slot; GameManager handles that
        var a = new EquipmentBonuses(Attack: 3);
        var b = new EquipmentBonuses(Attack: 7);
        var result = EquipmentLogic.SumBonuses([a, b]);
        Assert.That(result.Attack, Is.EqualTo(10));
    }

    // ── EquipmentBonuses record equality ──────────────────────────────────────

    [Test]
    public void EquipmentBonuses_DefaultEquality()
    {
        var a = new EquipmentBonuses();
        var b = new EquipmentBonuses();
        Assert.That(a, Is.EqualTo(b));
        Assert.That(new EquipmentBonuses(Attack: 5), Is.Not.EqualTo(new EquipmentBonuses(Attack: 6)));
    }
}
