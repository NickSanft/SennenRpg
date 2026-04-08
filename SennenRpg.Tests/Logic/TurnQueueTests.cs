using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Collections.Generic;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class TurnQueueTests
{
    private static (int, bool)[] Speeds(params int[] values)
    {
        var arr = new (int, bool)[values.Length];
        for (int i = 0; i < values.Length; i++) arr[i] = (values[i], false);
        return arr;
    }

    [Test]
    public void BuildOrder_SortsBySpeedDescending()
    {
        var party   = Speeds(8);                  // Sen speed 8
        var enemies = Speeds(12, 4);              // Enemy0 speed 12, Enemy1 speed 4
        var queue   = TurnQueue.BuildOrder(party, enemies);

        Assert.That(queue, Has.Count.EqualTo(3));
        // Speed 12 first → Enemy0
        Assert.That(queue[0].IsParty, Is.False);
        Assert.That(queue[0].Index,   Is.EqualTo(0));
        // Speed 8 next → Sen
        Assert.That(queue[1].IsParty, Is.True);
        Assert.That(queue[1].Index,   Is.EqualTo(0));
        // Speed 4 last → Enemy1
        Assert.That(queue[2].IsParty, Is.False);
        Assert.That(queue[2].Index,   Is.EqualTo(1));
    }

    [Test]
    public void BuildOrder_PartyWinsTieBreak()
    {
        // Sen and Enemy0 both have speed 10. Party should act first.
        var party   = Speeds(10);
        var enemies = Speeds(10);
        var queue   = TurnQueue.BuildOrder(party, enemies);
        Assert.That(queue[0].IsParty, Is.True);
        Assert.That(queue[1].IsParty, Is.False);
    }

    [Test]
    public void BuildOrder_SkipsKOdActors()
    {
        var party   = new (int, bool)[] { (10, false), (15, true), (8, false) }; // Lily KO'd
        var enemies = new (int, bool)[] { (12, false), (5, true) };               // Enemy1 KO'd
        var queue   = TurnQueue.BuildOrder(party, enemies);

        Assert.That(queue, Has.Count.EqualTo(3));
        // Sorted: Enemy0 (12), Sen (10), Rain (8)
        Assert.That(queue[0].IsParty, Is.False);
        Assert.That(queue[0].Index,   Is.EqualTo(0));
        Assert.That(queue[1].IsParty, Is.True);
        Assert.That(queue[1].Index,   Is.EqualTo(0));
        Assert.That(queue[2].IsParty, Is.True);
        Assert.That(queue[2].Index,   Is.EqualTo(2));
    }

    [Test]
    public void BuildOrder_PartyTieBreakUsesIndex()
    {
        // Three party members all at speed 10 — they should act in index order.
        var party   = Speeds(10, 10, 10);
        var enemies = new (int, bool)[0];
        var queue   = TurnQueue.BuildOrder(party, enemies);

        Assert.That(queue, Has.Count.EqualTo(3));
        Assert.That(queue[0].Index, Is.EqualTo(0));
        Assert.That(queue[1].Index, Is.EqualTo(1));
        Assert.That(queue[2].Index, Is.EqualTo(2));
    }

    [Test]
    public void BuildOrder_AllKO_ReturnsEmpty()
    {
        var party   = new (int, bool)[] { (10, true) };
        var enemies = new (int, bool)[] { (12, true) };
        var queue   = TurnQueue.BuildOrder(party, enemies);
        Assert.That(queue, Is.Empty);
    }

    [Test]
    public void BuildOrder_NullInputs_NoCrash()
    {
        var queue = TurnQueue.BuildOrder(null!, null!);
        Assert.That(queue, Is.Empty);
    }

    [Test]
    public void BuildOrder_FullPartyVsTwoEnemies()
    {
        // Sen (10) + Lily (8 — Alchemist) + Rain (15 — Rogue) vs Wisplet (5) + Centiphantom (12).
        // Expected order: Rain (15) → Centiphantom (12) → Sen (10) → Lily (8) → Wisplet (5).
        var party   = Speeds(10, 8, 15);
        var enemies = Speeds(5, 12);
        var queue   = TurnQueue.BuildOrder(party, enemies);

        Assert.That(queue, Has.Count.EqualTo(5));
        Assert.That(queue[0], Is.EqualTo(new TurnQueueEntry(true,  2, 15))); // Rain
        Assert.That(queue[1], Is.EqualTo(new TurnQueueEntry(false, 1, 12))); // Centiphantom
        Assert.That(queue[2], Is.EqualTo(new TurnQueueEntry(true,  0, 10))); // Sen
        Assert.That(queue[3], Is.EqualTo(new TurnQueueEntry(true,  1, 8)));  // Lily
        Assert.That(queue[4], Is.EqualTo(new TurnQueueEntry(false, 0, 5)));  // Wisplet
    }
}
