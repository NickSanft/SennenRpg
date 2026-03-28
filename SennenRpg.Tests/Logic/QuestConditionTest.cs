using NUnit.Framework;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class QuestConditionTest
{
    // ── Shared test state ─────────────────────────────────────────────

    private static readonly IReadOnlyDictionary<string, bool> NoFlags  = new Dictionary<string, bool>();
    private static readonly IReadOnlyDictionary<string, int>  NoKills  = new Dictionary<string, int>();

    private static IReadOnlyDictionary<string, bool> Flags(params (string k, bool v)[] entries)
    {
        var d = new Dictionary<string, bool>();
        foreach (var (k, v) in entries) d[k] = v;
        return d;
    }

    private static IReadOnlyDictionary<string, int> Kills(params (string k, int v)[] entries)
    {
        var d = new Dictionary<string, int>();
        foreach (var (k, v) in entries) d[k] = v;
        return d;
    }

    // ── KillCount ────────────────────────────────────────────────────

    [Test]
    public void KillCount_Unsatisfied_WhenZeroKills()
    {
        var cond = new QuestCondition(QuestConditionType.KillCount, "wisplet", 1);
        Assert.That(QuestLogic.IsSatisfied(cond, NoFlags, NoKills), Is.False);
    }

    [Test]
    public void KillCount_Satisfied_WhenExactCount()
    {
        var cond  = new QuestCondition(QuestConditionType.KillCount, "wisplet", 1);
        var kills = Kills(("wisplet", 1));
        Assert.That(QuestLogic.IsSatisfied(cond, NoFlags, kills), Is.True);
    }

    [Test]
    public void KillCount_Satisfied_WhenCountExceeded()
    {
        var cond  = new QuestCondition(QuestConditionType.KillCount, "wisplet", 1);
        var kills = Kills(("wisplet", 5));
        Assert.That(QuestLogic.IsSatisfied(cond, NoFlags, kills), Is.True);
    }

    [Test]
    public void KillCount_Unsatisfied_WhenBelowRequired()
    {
        var cond  = new QuestCondition(QuestConditionType.KillCount, "wisplet", 3);
        var kills = Kills(("wisplet", 2));
        Assert.That(QuestLogic.IsSatisfied(cond, NoFlags, kills), Is.False);
    }

    [Test]
    public void KillCount_Unsatisfied_WhenWrongEnemy()
    {
        var cond  = new QuestCondition(QuestConditionType.KillCount, "wisplet", 1);
        var kills = Kills(("goblin", 5));
        Assert.That(QuestLogic.IsSatisfied(cond, NoFlags, kills), Is.False);
    }

    // ── Flag ─────────────────────────────────────────────────────────

    [Test]
    public void Flag_Satisfied_WhenFlagIsTrue()
    {
        var cond  = new QuestCondition(QuestConditionType.Flag, "visited_cave");
        var flags = Flags(("visited_cave", true));
        Assert.That(QuestLogic.IsSatisfied(cond, flags, NoKills), Is.True);
    }

    [Test]
    public void Flag_Unsatisfied_WhenFlagIsFalse()
    {
        var cond  = new QuestCondition(QuestConditionType.Flag, "visited_cave");
        var flags = Flags(("visited_cave", false));
        Assert.That(QuestLogic.IsSatisfied(cond, flags, NoKills), Is.False);
    }

    [Test]
    public void Flag_Unsatisfied_WhenFlagMissing()
    {
        var cond = new QuestCondition(QuestConditionType.Flag, "visited_cave");
        Assert.That(QuestLogic.IsSatisfied(cond, NoFlags, NoKills), Is.False);
    }

    // ── TalkTo ───────────────────────────────────────────────────────

    [Test]
    public void TalkTo_Satisfied_WhenTalkedToFlagSet()
    {
        var cond  = new QuestCondition(QuestConditionType.TalkTo, "rork");
        var flags = Flags(("talked_to_rork", true));
        Assert.That(QuestLogic.IsSatisfied(cond, flags, NoKills), Is.True);
    }

    [Test]
    public void TalkTo_Unsatisfied_WhenNotTalked()
    {
        var cond = new QuestCondition(QuestConditionType.TalkTo, "rork");
        Assert.That(QuestLogic.IsSatisfied(cond, NoFlags, NoKills), Is.False);
    }

    [Test]
    public void TalkTo_Unsatisfied_WhenFlagIsFalse()
    {
        var cond  = new QuestCondition(QuestConditionType.TalkTo, "rork");
        var flags = Flags(("talked_to_rork", false));
        Assert.That(QuestLogic.IsSatisfied(cond, flags, NoKills), Is.False);
    }

    // ── AreAllConditionsMet ───────────────────────────────────────────

    [Test]
    public void AreAllConditionsMet_EmptyList_ReturnsTrue()
    {
        var result = QuestLogic.AreAllConditionsMet(
            new List<QuestCondition>(), NoFlags, NoKills);
        Assert.That(result, Is.True);
    }

    [Test]
    public void AreAllConditionsMet_AllSatisfied_ReturnsTrue()
    {
        var conds = new List<QuestCondition>
        {
            new(QuestConditionType.KillCount, "wisplet", 1),
            new(QuestConditionType.Flag,      "visited_cave"),
        };
        var kills = Kills(("wisplet", 1));
        var flags = Flags(("visited_cave", true));

        Assert.That(QuestLogic.AreAllConditionsMet(conds, flags, kills), Is.True);
    }

    [Test]
    public void AreAllConditionsMet_OneFails_ReturnsFalse()
    {
        var conds = new List<QuestCondition>
        {
            new(QuestConditionType.KillCount, "wisplet", 1),
            new(QuestConditionType.Flag,      "visited_cave"),
        };
        var kills = Kills(("wisplet", 1));
        // visited_cave flag NOT set

        Assert.That(QuestLogic.AreAllConditionsMet(conds, NoFlags, kills), Is.False);
    }

    [Test]
    public void AreAllConditionsMet_AllFail_ReturnsFalse()
    {
        var conds = new List<QuestCondition>
        {
            new(QuestConditionType.KillCount, "wisplet", 1),
            new(QuestConditionType.TalkTo,    "rork"),
        };
        Assert.That(QuestLogic.AreAllConditionsMet(conds, NoFlags, NoKills), Is.False);
    }
}
