using NUnit.Framework;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// Tests for QuestLogic with multi-kill requirements and mixed condition types,
/// simulating accumulating game state over time.
/// </summary>
[TestFixture]
public class QuestStateTest
{
    // ── Multi-kill requirement ────────────────────────────────────────

    [Test]
    public void MultipleKillsRequired_NotMetUntilCountReached()
    {
        var cond = new QuestCondition(QuestConditionType.KillCount, "wisplet", 3);
        var conditions = new List<QuestCondition> { cond };
        var flags = new Dictionary<string, bool>();

        for (int kills = 0; kills < 3; kills++)
        {
            var killCounts = new Dictionary<string, int> { ["wisplet"] = kills };
            bool met = QuestLogic.AreAllConditionsMet(conditions, flags, killCounts);
            Assert.That(met, Is.False, $"Should not be met at {kills} kills");
        }

        var exactKillCounts = new Dictionary<string, int> { ["wisplet"] = 3 };
        Assert.That(QuestLogic.AreAllConditionsMet(conditions, flags, exactKillCounts), Is.True,
            "Should be met at exactly 3 kills");
    }

    // ── Mixed conditions: kill + flag ─────────────────────────────────

    [Test]
    public void MixedConditions_KillAndFlag_BothMustBeTrue()
    {
        var conditions = new List<QuestCondition>
        {
            new(QuestConditionType.KillCount, "wisplet",      1),
            new(QuestConditionType.Flag,      "talked_to_rork"),
        };

        // Only kills satisfied
        Assert.That(
            QuestLogic.AreAllConditionsMet(
                conditions,
                new Dictionary<string, bool>(),
                new Dictionary<string, int> { ["wisplet"] = 1 }),
            Is.False, "Kill alone is not enough");

        // Only flag satisfied
        Assert.That(
            QuestLogic.AreAllConditionsMet(
                conditions,
                new Dictionary<string, bool> { ["talked_to_rork"] = true },
                new Dictionary<string, int>()),
            Is.False, "Flag alone is not enough");

        // Both satisfied
        Assert.That(
            QuestLogic.AreAllConditionsMet(
                conditions,
                new Dictionary<string, bool> { ["talked_to_rork"] = true },
                new Dictionary<string, int> { ["wisplet"] = 1 }),
            Is.True, "Both conditions met should return true");
    }

    // ── Kill count with multiple enemy types ─────────────────────────

    [Test]
    public void KillCount_DistinguishesBetweenEnemyTypes()
    {
        var cond       = new QuestCondition(QuestConditionType.KillCount, "wisplet", 1);
        var conditions = new List<QuestCondition> { cond };
        var flags      = new Dictionary<string, bool>();

        // Killed a different enemy — should not count
        var wrongKills = new Dictionary<string, int> { ["goblin"] = 5 };
        Assert.That(QuestLogic.AreAllConditionsMet(conditions, flags, wrongKills), Is.False);

        // Killed the right enemy — should count
        var rightKills = new Dictionary<string, int> { ["wisplet"] = 1, ["goblin"] = 5 };
        Assert.That(QuestLogic.AreAllConditionsMet(conditions, flags, rightKills), Is.True);
    }

    // ── IsSatisfied default count=1 ──────────────────────────────────

    [Test]
    public void QuestCondition_DefaultCount_IsOne()
    {
        var cond = new QuestCondition(QuestConditionType.KillCount, "wisplet");
        Assert.That(cond.Count, Is.EqualTo(1));
    }

    // ── Unknown condition type ────────────────────────────────────────

    [Test]
    public void IsSatisfied_UnknownType_ReturnsFalse()
    {
        // Cast an out-of-range int to the enum to simulate an unknown type
        var cond = new QuestCondition((QuestConditionType)999, "anything", 1);
        Assert.That(QuestLogic.IsSatisfied(cond, new Dictionary<string, bool>(),
                                                  new Dictionary<string, int>()), Is.False);
    }
}
