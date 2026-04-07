using System.Collections.Generic;
using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class BestiaryLogicTests
{
    // ── TierFor ──────────────────────────────────────────────────────────

    [TestCase(0, BestiaryLogic.EntryTier.Locked)]
    [TestCase(1, BestiaryLogic.EntryTier.Discovered)]
    [TestCase(2, BestiaryLogic.EntryTier.Discovered)]
    [TestCase(3, BestiaryLogic.EntryTier.Studied)]
    [TestCase(50, BestiaryLogic.EntryTier.Studied)]
    public void TierFor_Boundaries(int killCount, BestiaryLogic.EntryTier expected)
    {
        Assert.That(BestiaryLogic.TierFor(killCount), Is.EqualTo(expected));
    }

    [Test]
    public void TierFor_NegativeKillCount_ReturnsLocked()
    {
        // Defensive — ResetForNewGame and other paths shouldn't ever produce
        // negative counts, but the math must be safe.
        Assert.That(BestiaryLogic.TierFor(-5), Is.EqualTo(BestiaryLogic.EntryTier.Locked));
    }

    // ── KillsUntilStudied ────────────────────────────────────────────────

    [TestCase(0, 3)]
    [TestCase(1, 2)]
    [TestCase(2, 1)]
    [TestCase(3, 0)]
    [TestCase(99, 0)]
    public void KillsUntilStudied_Boundaries(int killCount, int expected)
    {
        Assert.That(BestiaryLogic.KillsUntilStudied(killCount), Is.EqualTo(expected));
    }

    // ── Completion ───────────────────────────────────────────────────────

    [Test]
    public void Completion_EmptyKills_ReturnsZeroOverTotal()
    {
        var ids = new[] { "a", "b", "c" };
        var kills = new Dictionary<string, int>();
        var (discovered, total) = BestiaryLogic.Completion(ids, kills);
        Assert.That(discovered, Is.EqualTo(0));
        Assert.That(total, Is.EqualTo(3));
    }

    [Test]
    public void Completion_AllDefeated_ReturnsAllOverTotal()
    {
        var ids = new[] { "a", "b", "c" };
        var kills = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 5,
            ["c"] = 100,
        };
        var (discovered, total) = BestiaryLogic.Completion(ids, kills);
        Assert.That(discovered, Is.EqualTo(3));
        Assert.That(total, Is.EqualTo(3));
    }

    [Test]
    public void Completion_ZeroKillsDoNotCount()
    {
        // Edge: a key in killCounts with value 0 should not be considered discovered.
        var ids = new[] { "a", "b" };
        var kills = new Dictionary<string, int> { ["a"] = 0, ["b"] = 1 };
        var (discovered, total) = BestiaryLogic.Completion(ids, kills);
        Assert.That(discovered, Is.EqualTo(1));
        Assert.That(total, Is.EqualTo(2));
    }

    [Test]
    public void Completion_MissingKeysAreLocked()
    {
        var ids = new[] { "a", "b", "c" };
        var kills = new Dictionary<string, int> { ["a"] = 1 };
        var (discovered, total) = BestiaryLogic.Completion(ids, kills);
        Assert.That(discovered, Is.EqualTo(1));
        Assert.That(total, Is.EqualTo(3));
    }

    [Test]
    public void Completion_EmptyEnemyList_ReturnsZeroZero()
    {
        var (discovered, total) = BestiaryLogic.Completion(
            System.Array.Empty<string>(),
            new Dictionary<string, int>());
        Assert.That(discovered, Is.EqualTo(0));
        Assert.That(total, Is.EqualTo(0));
    }

    // ── MasteryFor ───────────────────────────────────────────────────────

    [TestCase(0,  BestiaryLogic.MasteryRank.None)]
    [TestCase(2,  BestiaryLogic.MasteryRank.None)]
    [TestCase(3,  BestiaryLogic.MasteryRank.Bronze)]
    [TestCase(9,  BestiaryLogic.MasteryRank.Bronze)]
    [TestCase(10, BestiaryLogic.MasteryRank.Silver)]
    [TestCase(24, BestiaryLogic.MasteryRank.Silver)]
    [TestCase(25, BestiaryLogic.MasteryRank.Gold)]
    [TestCase(49, BestiaryLogic.MasteryRank.Gold)]
    [TestCase(50, BestiaryLogic.MasteryRank.Platinum)]
    [TestCase(999, BestiaryLogic.MasteryRank.Platinum)]
    public void MasteryFor_Thresholds(int killCount, BestiaryLogic.MasteryRank expected)
    {
        Assert.That(BestiaryLogic.MasteryFor(killCount), Is.EqualTo(expected));
    }
}
