using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// NUnit tests for ItemLogic — the pure item-use calculations.
/// No Godot runtime required.
/// </summary>
[TestFixture]
public sealed class ItemLogicTests
{
    // ── CanUseItem ────────────────────────────────────────────────────────────

    [Test]
    public void CanUseItem_HealItem_BelowMaxHp_ReturnsTrue()
        => Assert.That(ItemLogic.CanUseItem(healAmount: 10, currentHp: 5, maxHp: 20), Is.True);

    [Test]
    public void CanUseItem_HealItem_AtMaxHp_ReturnsFalse()
        => Assert.That(ItemLogic.CanUseItem(healAmount: 10, currentHp: 20, maxHp: 20), Is.False);

    [Test]
    public void CanUseItem_ZeroHealAmount_ReturnsFalse()
        => Assert.That(ItemLogic.CanUseItem(healAmount: 0, currentHp: 5, maxHp: 20), Is.False);

    [Test]
    public void CanUseItem_NegativeHealAmount_ReturnsFalse()
        => Assert.That(ItemLogic.CanUseItem(healAmount: -5, currentHp: 5, maxHp: 20), Is.False);

    [Test]
    public void CanUseItem_HpIsZero_ReturnsTrue()
        => Assert.That(ItemLogic.CanUseItem(healAmount: 10, currentHp: 0, maxHp: 20), Is.True);

    [Test]
    public void CanUseItem_HpOneBeforeMax_ReturnsTrue()
        => Assert.That(ItemLogic.CanUseItem(healAmount: 10, currentHp: 19, maxHp: 20), Is.True);

    // ── ApplyHeal ─────────────────────────────────────────────────────────────

    [Test]
    public void ApplyHeal_NormalCase_AddsHealAmount()
        => Assert.That(ItemLogic.ApplyHeal(healAmount: 10, currentHp: 5, maxHp: 20), Is.EqualTo(15));

    [Test]
    public void ApplyHeal_WouldExceedMax_ClampsToMax()
        => Assert.That(ItemLogic.ApplyHeal(healAmount: 100, currentHp: 15, maxHp: 20), Is.EqualTo(20));

    [Test]
    public void ApplyHeal_ExactMax_ReturnsMax()
        => Assert.That(ItemLogic.ApplyHeal(healAmount: 5, currentHp: 15, maxHp: 20), Is.EqualTo(20));

    [Test]
    public void ApplyHeal_AlreadyAtMax_ReturnsMax()
        => Assert.That(ItemLogic.ApplyHeal(healAmount: 10, currentHp: 20, maxHp: 20), Is.EqualTo(20));

    [Test]
    public void ApplyHeal_ZeroHp_ReturnsHealAmount()
        => Assert.That(ItemLogic.ApplyHeal(healAmount: 10, currentHp: 0, maxHp: 20), Is.EqualTo(10));

    // ── ActualHeal ────────────────────────────────────────────────────────────

    [Test]
    public void ActualHeal_NormalCase_ReturnsFullAmount()
        => Assert.That(ItemLogic.ActualHeal(healAmount: 10, currentHp: 5, maxHp: 20), Is.EqualTo(10));

    [Test]
    public void ActualHeal_NearMax_ReturnsDifference()
        => Assert.That(ItemLogic.ActualHeal(healAmount: 10, currentHp: 15, maxHp: 20), Is.EqualTo(5));

    [Test]
    public void ActualHeal_AtMax_ReturnsZero()
        => Assert.That(ItemLogic.ActualHeal(healAmount: 10, currentHp: 20, maxHp: 20), Is.EqualTo(0));

    [Test]
    public void ActualHeal_ZeroHp_ReturnsFullAmount()
        => Assert.That(ItemLogic.ActualHeal(healAmount: 10, currentHp: 0, maxHp: 20), Is.EqualTo(10));

    // ── Consistency ───────────────────────────────────────────────────────────

    [TestCase(10, 5,  20)]
    [TestCase(10, 15, 20)]
    [TestCase(10, 20, 20)]
    [TestCase(10, 0,  10)]
    public void ActualHeal_PlusCurrentHp_EqualsApplyHeal(int heal, int current, int max)
    {
        int actual  = ItemLogic.ActualHeal(heal, current, max);
        int applied = ItemLogic.ApplyHeal(heal, current, max);
        Assert.That(current + actual, Is.EqualTo(applied),
            "ActualHeal + currentHp must equal ApplyHeal.");
    }
}
