using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// NUnit tests for ShopLogic — the pure shop transaction calculations.
/// No Godot runtime required.
/// </summary>
[TestFixture]
public sealed class ShopLogicTests
{
    // ── CanAfford ─────────────────────────────────────────────────────────────

    [Test]
    public void CanAfford_GoldEqualsPrice_ReturnsTrue()
        => Assert.That(ShopLogic.CanAfford(gold: 50, price: 50), Is.True);

    [Test]
    public void CanAfford_GoldExceedsPrice_ReturnsTrue()
        => Assert.That(ShopLogic.CanAfford(gold: 100, price: 50), Is.True);

    [Test]
    public void CanAfford_GoldBelowPrice_ReturnsFalse()
        => Assert.That(ShopLogic.CanAfford(gold: 30, price: 50), Is.False);

    [Test]
    public void CanAfford_ZeroGold_ZeroPrice_ReturnsTrue()
        => Assert.That(ShopLogic.CanAfford(gold: 0, price: 0), Is.True);

    [Test]
    public void CanAfford_ZeroGold_NonZeroPrice_ReturnsFalse()
        => Assert.That(ShopLogic.CanAfford(gold: 0, price: 10), Is.False);

    // ── GoldAfterPurchase ─────────────────────────────────────────────────────

    [Test]
    public void GoldAfterPurchase_DeductsPrice()
        => Assert.That(ShopLogic.GoldAfterPurchase(gold: 100, price: 30), Is.EqualTo(70));

    [Test]
    public void GoldAfterPurchase_ExactAmount_ReturnsZero()
        => Assert.That(ShopLogic.GoldAfterPurchase(gold: 50, price: 50), Is.EqualTo(0));

    [Test]
    public void GoldAfterPurchase_FreeItem_ReturnsFullGold()
        => Assert.That(ShopLogic.GoldAfterPurchase(gold: 50, price: 0), Is.EqualTo(50));

    // ── Consistency ───────────────────────────────────────────────────────────

    [TestCase(100, 30)]
    [TestCase(50,  50)]
    [TestCase(10,   5)]
    public void GoldAfterPurchase_IsNonNegative_WhenCanAfford(int gold, int price)
    {
        Assert.That(ShopLogic.CanAfford(gold, price), Is.True,
            "Precondition: player can afford this item.");
        Assert.That(ShopLogic.GoldAfterPurchase(gold, price), Is.GreaterThanOrEqualTo(0),
            "Remaining gold must never be negative after an affordable purchase.");
    }
}
