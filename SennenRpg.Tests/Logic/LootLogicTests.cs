using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class LootLogicTests
{
    // ── RollLoot — basic cases ─────────────────────────────────────────────────

    [Test]
    public void RollLoot_EmptyTable_ReturnsNull()
    {
        var result = LootLogic.RollLoot([], [], [], () => 0f);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void RollLoot_SingleEntry_AlwaysReturnsIt()
    {
        var result = LootLogic.RollLoot(["item_a"], [1], [false], () => 0f);
        Assert.That(result, Is.EqualTo("item_a"));
    }

    [Test]
    public void RollLoot_AllZeroWeights_ReturnsNull()
    {
        var result = LootLogic.RollLoot(["item_a", "item_b"], [0, 0], [false, false], () => 0f);
        Assert.That(result, Is.Null);
    }

    // ── Guaranteed entries ─────────────────────────────────────────────────────

    [Test]
    public void RollLoot_GuaranteedEntry_AlwaysSelected()
    {
        // Regardless of roll value, the guaranteed entry wins
        for (float roll = 0f; roll < 1f; roll += 0.1f)
        {
            float captured = roll;
            var result = LootLogic.RollLoot(
                ["item_a", "item_b", "guaranteed_item"],
                [10, 10, 1],
                [false, false, true],
                () => captured);
            Assert.That(result, Is.EqualTo("guaranteed_item"), $"Roll={captured}");
        }
    }

    [Test]
    public void RollLoot_FirstGuaranteedWins()
    {
        var result = LootLogic.RollLoot(
            ["g1", "g2"],
            [1, 1],
            [true, true],
            () => 0f);
        Assert.That(result, Is.EqualTo("g1"));
    }

    // ── Weighted distribution ──────────────────────────────────────────────────

    [Test]
    public void RollLoot_EqualWeights_LowRollPicksFirst()
    {
        // Total weight = 2. Roll 0.0 → acc hits item_a at 1.0
        var result = LootLogic.RollLoot(["item_a", "item_b"], [1, 1], [false, false], () => 0f);
        Assert.That(result, Is.EqualTo("item_a"));
    }

    [Test]
    public void RollLoot_EqualWeights_HighRollPicksSecond()
    {
        // Total weight = 2. Roll 0.999 * 2 = 1.998 → past item_a (acc=1), picks item_b
        var result = LootLogic.RollLoot(["item_a", "item_b"], [1, 1], [false, false], () => 0.999f);
        Assert.That(result, Is.EqualTo("item_b"));
    }

    [Test]
    public void RollLoot_HighWeightEntryDominates()
    {
        // item_a weight=9, item_b weight=1. Roll must be in top 10% to pick item_b.
        int countA = 0, countB = 0;
        float[] rolls = [0.0f, 0.1f, 0.2f, 0.5f, 0.85f, 0.91f, 0.95f, 0.99f];
        foreach (float r in rolls)
        {
            float captured = r;
            var res = LootLogic.RollLoot(["item_a", "item_b"], [9, 1], [false, false], () => captured);
            if (res == "item_a") countA++;
            else countB++;
        }
        // 90% weight on item_a → should dominate across a spread of rolls
        Assert.That(countA, Is.GreaterThan(countB));
    }

    // ── TotalWeight ────────────────────────────────────────────────────────────

    [Test]
    public void TotalWeight_ExcludesGuaranteedEntries()
    {
        int total = LootLogic.TotalWeight([5, 3, 10], [false, true, false]);
        Assert.That(total, Is.EqualTo(15)); // 5 + 10 only
    }

    [Test]
    public void TotalWeight_AllGuaranteed_ReturnsZero()
    {
        int total = LootLogic.TotalWeight([5, 5], [true, true]);
        Assert.That(total, Is.EqualTo(0));
    }

    [Test]
    public void TotalWeight_EmptyArrays_ReturnsZero()
    {
        Assert.That(LootLogic.TotalWeight([], []), Is.EqualTo(0));
    }

    // ── ComputeProbabilities ───────────────────────────────────────────────────

    [Test]
    public void ComputeProbabilities_EqualWeights_Returns50Percent()
    {
        float[] probs = LootLogic.ComputeProbabilities([1, 1], [false, false]);
        Assert.That(probs[0], Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(probs[1], Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void ComputeProbabilities_GuaranteedEntry_Returns1()
    {
        float[] probs = LootLogic.ComputeProbabilities([1, 5], [true, false]);
        Assert.That(probs[0], Is.EqualTo(1f));
    }
}
