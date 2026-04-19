using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class PartyReactionTests
{
    // ── Registry validation ──────────────────────────────────────────

    [Test]
    public void Registry_NoEmptyText()
    {
        foreach (var r in PartyReactionRegistry.All)
            Assert.That(r.Text, Is.Not.Null.And.Not.Empty, $"Reaction for {r.MemberId}@{r.MapId} has empty text");
    }

    [Test]
    public void Registry_NoEmptyMemberId()
    {
        foreach (var r in PartyReactionRegistry.All)
            Assert.That(r.MemberId, Is.Not.Null.And.Not.Empty, $"Reaction on {r.MapId} has empty MemberId");
    }

    [Test]
    public void Registry_AllPrioritiesPositive()
    {
        foreach (var r in PartyReactionRegistry.All)
            Assert.That(r.Priority, Is.GreaterThan(0), $"Reaction '{r.Text}' has non-positive priority");
    }

    [Test]
    public void Registry_HasMultipleCharacters()
    {
        var ids = PartyReactionRegistry.All.Select(r => r.MemberId).Distinct().ToList();
        Assert.That(ids.Count, Is.GreaterThanOrEqualTo(4));
    }

    // ── GetNextReaction ──────────────────────────────────────────────

    [Test]
    public void GetNextReaction_UnknownMap_ReturnsNull()
    {
        var result = PartyReactionLogic.GetNextReaction(
            "nonexistent_map",
            new List<string> { "lily", "rain" },
            new HashSet<string>());

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetNextReaction_AllShown_ReturnsNull()
    {
        var members = new List<string> { "lily" };
        var shown = new HashSet<string>();

        // Show every lily reaction
        foreach (var r in PartyReactionRegistry.All)
        {
            if (r.MemberId == "lily")
                shown.Add(PartyReactionLogic.ReactionKey(r));
        }

        var result = PartyReactionLogic.GetNextReaction("mapp_tavern", members, shown);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetNextReaction_ReturnsHighestPriorityFirst()
    {
        var members = new List<string> { "lily" };
        var shown = new HashSet<string>();

        var result = PartyReactionLogic.GetNextReaction("mapp_tavern", members, shown);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Priority, Is.EqualTo(3));
    }

    [Test]
    public void GetNextReaction_FiltersInactiveMember()
    {
        // Only rain is active, but we look for lily reactions
        var members = new List<string> { "rain" };
        var shown = new HashSet<string>();

        var result = PartyReactionLogic.GetNextReaction("mapp_tavern", members, shown);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.MemberId, Is.EqualTo("rain"));
    }

    [Test]
    public void GetNextReaction_SkipsShownReactions()
    {
        var members = new List<string> { "lily" };
        var shown = new HashSet<string>();

        // Get first reaction and mark it shown
        var first = PartyReactionLogic.GetNextReaction("mapp_tavern", members, shown);
        Assert.That(first, Is.Not.Null);
        shown.Add(PartyReactionLogic.ReactionKey(first!.Value));

        // Second should be different
        var second = PartyReactionLogic.GetNextReaction("mapp_tavern", members, shown);
        Assert.That(second, Is.Not.Null);
        Assert.That(second!.Value.Text, Is.Not.EqualTo(first.Value.Text));
    }

    // ── ReactionKey ──────────────────────────────────────────────────

    [Test]
    public void ReactionKey_IsDeterministic()
    {
        var r = new PartyReaction("lily", "world_map", "Hello!", 1);
        var k1 = PartyReactionLogic.ReactionKey(r);
        var k2 = PartyReactionLogic.ReactionKey(r);
        Assert.That(k1, Is.EqualTo(k2));
    }

    [Test]
    public void ReactionKey_UniquePerReaction()
    {
        var keys = PartyReactionRegistry.All
            .Select(PartyReactionLogic.ReactionKey)
            .ToList();
        Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Count),
            "All registry reactions must produce unique keys");
    }

    // ── ShouldCheckReactions ─────────────────────────────────────────

    [Test]
    public void ShouldCheckReactions_TrueAtInterval()
    {
        Assert.That(PartyReactionLogic.ShouldCheckReactions(15), Is.True);
    }

    [Test]
    public void ShouldCheckReactions_TrueAboveInterval()
    {
        Assert.That(PartyReactionLogic.ShouldCheckReactions(20), Is.True);
    }

    [Test]
    public void ShouldCheckReactions_FalseBeforeInterval()
    {
        Assert.That(PartyReactionLogic.ShouldCheckReactions(14), Is.False);
    }

    [Test]
    public void ShouldCheckReactions_CustomInterval()
    {
        Assert.That(PartyReactionLogic.ShouldCheckReactions(9, 10), Is.False);
        Assert.That(PartyReactionLogic.ShouldCheckReactions(10, 10), Is.True);
    }
}
