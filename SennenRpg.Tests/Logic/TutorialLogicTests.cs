using System.Collections.Generic;
using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class TutorialLogicTests
{
    // ── ShouldShow ──────────────────────────────────────────────────────

    [Test]
    public void ShouldShow_KnownUnseen_ReturnsTrue()
    {
        var seen = new HashSet<string>();
        Assert.That(
            TutorialLogic.ShouldShow(TutorialIds.OverworldMovement, skipEnabled: false, seen),
            Is.True);
    }

    [Test]
    public void ShouldShow_SkipEnabled_ReturnsFalse()
    {
        var seen = new HashSet<string>();
        Assert.That(
            TutorialLogic.ShouldShow(TutorialIds.OverworldMovement, skipEnabled: true, seen),
            Is.False);
    }

    [Test]
    public void ShouldShow_AlreadySeen_ReturnsFalse()
    {
        var seen = new HashSet<string> { TutorialIds.BattleFight };
        Assert.That(
            TutorialLogic.ShouldShow(TutorialIds.BattleFight, skipEnabled: false, seen),
            Is.False);
    }

    [Test]
    public void ShouldShow_UnknownId_ReturnsFalse()
    {
        var seen = new HashSet<string>();
        Assert.That(
            TutorialLogic.ShouldShow("not_a_real_tutorial_id", skipEnabled: false, seen),
            Is.False);
    }

    [Test]
    public void ShouldShow_EmptyId_ReturnsFalse()
    {
        var seen = new HashSet<string>();
        Assert.That(TutorialLogic.ShouldShow("", skipEnabled: false, seen), Is.False);
        Assert.That(TutorialLogic.ShouldShow(null!, skipEnabled: false, seen), Is.False);
    }

    [Test]
    public void ShouldShow_SkipTakesPrecedenceOverSeen()
    {
        // Both conditions active — result is still False (either would make it False).
        var seen = new HashSet<string> { TutorialIds.Cooking };
        Assert.That(
            TutorialLogic.ShouldShow(TutorialIds.Cooking, skipEnabled: true, seen),
            Is.False);
    }

    // ── Find ───────────────────────────────────────────────────────────

    [Test]
    public void Find_KnownId_ReturnsEntry()
    {
        var t = TutorialLogic.Find(TutorialIds.OverworldMovement);
        Assert.That(t, Is.Not.Null);
        Assert.That(t!.Value.Id, Is.EqualTo(TutorialIds.OverworldMovement));
        Assert.That(t.Value.Title, Is.Not.Empty);
        Assert.That(t.Value.Body,  Is.Not.Empty);
    }

    [Test]
    public void Find_UnknownId_ReturnsNull()
    {
        Assert.That(TutorialLogic.Find("does_not_exist"), Is.Null);
    }

    [Test]
    public void Find_EmptyId_ReturnsNull()
    {
        Assert.That(TutorialLogic.Find(""), Is.Null);
        Assert.That(TutorialLogic.Find(null!), Is.Null);
    }

    // ── ValidateRegistry ────────────────────────────────────────────────

    [Test]
    public void ValidateRegistry_AllRealEntries_NoErrors()
    {
        bool ok = TutorialLogic.ValidateRegistry(TutorialRegistry.All, out var errors);
        Assert.That(ok, Is.True, "Registry errors: " + string.Join(", ", errors));
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ValidateRegistry_DuplicateIds_ReportsError()
    {
        var entries = new[]
        {
            new Tutorial("a", "A", "Body a", TutorialCategory.Overworld),
            new Tutorial("a", "A2", "Body a2", TutorialCategory.Battle),
        };
        bool ok = TutorialLogic.ValidateRegistry(entries, out var errors);
        Assert.That(ok, Is.False);
        Assert.That(errors, Has.Some.Contains("Duplicate"));
    }

    [Test]
    public void ValidateRegistry_EmptyTitle_ReportsError()
    {
        var entries = new[]
        {
            new Tutorial("x", "", "Body", TutorialCategory.Overworld),
        };
        bool ok = TutorialLogic.ValidateRegistry(entries, out var errors);
        Assert.That(ok, Is.False);
        Assert.That(errors, Has.Some.Contains("Title"));
    }

    [Test]
    public void ValidateRegistry_EmptyBody_ReportsError()
    {
        var entries = new[]
        {
            new Tutorial("x", "Title", "", TutorialCategory.Overworld),
        };
        bool ok = TutorialLogic.ValidateRegistry(entries, out var errors);
        Assert.That(ok, Is.False);
        Assert.That(errors, Has.Some.Contains("Body"));
    }

    [Test]
    public void ValidateRegistry_EmptyId_ReportsError()
    {
        var entries = new[]
        {
            new Tutorial("", "Title", "Body", TutorialCategory.Overworld),
        };
        bool ok = TutorialLogic.ValidateRegistry(entries, out var errors);
        Assert.That(ok, Is.False);
        Assert.That(errors, Has.Some.Contains("empty Id"));
    }

    // ── Registry contract: every declared TutorialId constant must exist ──

    [TestCase(TutorialIds.OverworldMovement)]
    [TestCase(TutorialIds.InteractPrompt)]
    [TestCase(TutorialIds.SavePoint)]
    [TestCase(TutorialIds.Foraging)]
    [TestCase(TutorialIds.WeatherFirst)]
    [TestCase(TutorialIds.Mode7)]
    [TestCase(TutorialIds.EncounterFirst)]
    [TestCase(TutorialIds.BattleFight)]
    [TestCase(TutorialIds.ComboSpell)]
    [TestCase(TutorialIds.RhythmStrike)]
    [TestCase(TutorialIds.RhythmDodge)]
    [TestCase(TutorialIds.ItemMenu)]
    [TestCase(TutorialIds.Equipment)]
    [TestCase(TutorialIds.SpellsMenu)]
    [TestCase(TutorialIds.Cooking)]
    [TestCase(TutorialIds.PartyRecruit)]
    [TestCase(TutorialIds.PartyMenu)]
    [TestCase(TutorialIds.ClassChange)]
    [TestCase(TutorialIds.Jukebox)]
    public void Registry_Contains_EveryDeclaredId(string id)
    {
        Assert.That(TutorialLogic.Find(id), Is.Not.Null,
            $"TutorialIds.{id} is declared but missing from TutorialRegistry.All");
    }

    [Test]
    public void Registry_HasReasonableSize()
    {
        // Guard-rail: if someone accidentally deletes the registry, the contract
        // tests above would all fail, but this test gives a clean signal too.
        Assert.That(TutorialRegistry.All.Length, Is.GreaterThanOrEqualTo(15));
    }
}
