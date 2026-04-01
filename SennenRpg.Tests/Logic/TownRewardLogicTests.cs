using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class TownRewardLogicTests
{
    // ── Counter behaviour ──────────────────────────────────────────────────────

    [Test]
    public void Counter_IncrementsBelowThreshold()
    {
        var result = TownRewardLogic.TryTick(0, false, 0, false, 0, 1);
        Assert.That(result.NewCounter, Is.EqualTo(1));
        Assert.That(result.RainTicked, Is.False);
        Assert.That(result.LilyTicked, Is.False);
    }

    [Test]
    public void Counter_ResetsAtTen()
    {
        var result = TownRewardLogic.TryTick(9, false, 0, false, 0, 1);
        Assert.That(result.NewCounter, Is.EqualTo(0));
    }

    [Test]
    public void Counter_DoesNotFireBeforeTen()
    {
        for (int i = 0; i < 9; i++)
        {
            var r = TownRewardLogic.TryTick(i, true, 0, true, 0, 1);
            Assert.That(r.RainTicked, Is.False, $"Step {i} should not tick");
        }
    }

    // ── Rain ──────────────────────────────────────────────────────────────────

    [Test]
    public void Rain_AddsGoldOnTick()
    {
        var result = TownRewardLogic.TryTick(9, rainPurchased: true, 0, false, 0, 1);
        Assert.That(result.RainTicked, Is.True);
        Assert.That(result.NewPendingRainGold, Is.EqualTo(TownRewardLogic.RainGoldPerTick));
    }

    [Test]
    public void Rain_DoesNotFireIfNotPurchased()
    {
        var result = TownRewardLogic.TryTick(9, rainPurchased: false, 0, false, 0, 1);
        Assert.That(result.RainTicked, Is.False);
        Assert.That(result.NewPendingRainGold, Is.EqualTo(0));
    }

    [Test]
    public void Rain_RespectsHardCap()
    {
        int atCap = TownRewardLogic.MaxPendingRainGold;
        var result = TownRewardLogic.TryTick(9, true, atCap, false, 0, 1);
        Assert.That(result.RainTicked, Is.False);
        Assert.That(result.NewPendingRainGold, Is.EqualTo(atCap));
    }

    [Test]
    public void Rain_ClampsToCapNotBeyond()
    {
        int nearCap = TownRewardLogic.MaxPendingRainGold - 5; // 5 below cap
        var result = TownRewardLogic.TryTick(9, true, nearCap, false, 0, 1);
        Assert.That(result.NewPendingRainGold, Is.EqualTo(TownRewardLogic.MaxPendingRainGold));
    }

    // ── Lily ──────────────────────────────────────────────────────────────────

    [Test]
    public void Lily_GeneratesRecipeOnTick()
    {
        var result = TownRewardLogic.TryTick(9, false, 0, lilyPurchased: true, 0, 5);
        Assert.That(result.LilyTicked, Is.True);
        Assert.That(result.LilyRecipe, Is.Not.Null.And.Not.Empty);
        Assert.That(result.NewPendingLilyCount, Is.EqualTo(1));
    }

    [Test]
    public void Lily_DoesNotFireIfNotPurchased()
    {
        var result = TownRewardLogic.TryTick(9, false, 0, lilyPurchased: false, 0, 1);
        Assert.That(result.LilyTicked, Is.False);
        Assert.That(result.LilyRecipe, Is.Null);
    }

    [Test]
    public void Lily_RespectsHardCap()
    {
        int atCap = TownRewardLogic.MaxPendingLilyItems;
        var result = TownRewardLogic.TryTick(9, false, 0, true, atCap, 1);
        Assert.That(result.LilyTicked, Is.False);
        Assert.That(result.NewPendingLilyCount, Is.EqualTo(atCap));
    }

    // ── HasPendingRewards ─────────────────────────────────────────────────────

    [Test]
    public void HasPendingRewards_TrueWhenRainGoldWaiting()
        => Assert.That(TownRewardLogic.HasPendingRewards(50, 0), Is.True);

    [Test]
    public void HasPendingRewards_TrueWhenLilyItemsWaiting()
        => Assert.That(TownRewardLogic.HasPendingRewards(0, 3), Is.True);

    [Test]
    public void HasPendingRewards_FalseWhenNothingPending()
        => Assert.That(TownRewardLogic.HasPendingRewards(0, 0), Is.False);
}
