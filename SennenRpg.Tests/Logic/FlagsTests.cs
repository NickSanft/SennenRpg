using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// NUnit tests for the Flags constants class.
/// No Godot runtime required — Flags is pure C# with no engine dependencies.
/// </summary>
[TestFixture]
public sealed class FlagsTests
{
    // ── Constant values ───────────────────────────────────────────────────────

    [Test]
    public void MetShizu_HasExpectedValue()
        => Assert.That(Flags.MetShizu, Is.EqualTo("met_shizu"));

    [Test]
    public void MetShizu_IsNotNullOrEmpty()
        => Assert.That(Flags.MetShizu, Is.Not.Null.And.Not.Empty);

    // ── All constants are unique ──────────────────────────────────────────────

    [Test]
    public void AllConstants_AreUnique()
    {
        // Add every public const string from Flags here.
        // A duplicate would indicate a copy-paste error.
        string[] all =
        [
            Flags.MetShizu,
            Flags.SeenNorthExitHint,
            Flags.BrixHorseAppeared,
            Flags.BhataFalafelAppeared,
            Flags.KrioraCrystalsAppeared,
            Flags.GusTransformedToFrog,
            Flags.ShizuMusicAuraActive,
            Flags.LilyAltDone,
            Flags.RainAltDone,
            Flags.AllAltDialogsDone,
            Flags.NpcKrioraPurchased,
        ];
        Assert.That(all, Is.Unique, "Each Flags constant must map to a distinct string.");
    }

    // ── New story flag values ─────────────────────────────────────────────────

    [Test]
    public void BrixHorseAppeared_HasExpectedValue()
        => Assert.That(Flags.BrixHorseAppeared, Is.EqualTo("brix_horse_appeared"));

    [Test]
    public void BhataFalafelAppeared_HasExpectedValue()
        => Assert.That(Flags.BhataFalafelAppeared, Is.EqualTo("bhata_falafel_appeared"));

    [Test]
    public void KrioraCrystalsAppeared_HasExpectedValue()
        => Assert.That(Flags.KrioraCrystalsAppeared, Is.EqualTo("kriora_crystals_appeared"));

    [Test]
    public void GusTransformedToFrog_HasExpectedValue()
        => Assert.That(Flags.GusTransformedToFrog, Is.EqualTo("gus_transformed_to_frog"));

    [Test]
    public void ShizuMusicAuraActive_HasExpectedValue()
        => Assert.That(Flags.ShizuMusicAuraActive, Is.EqualTo("shizu_music_aura_active"));

    [Test]
    public void LilyAltDone_HasExpectedValue()
        => Assert.That(Flags.LilyAltDone, Is.EqualTo("lily_alt_done"));

    [Test]
    public void RainAltDone_HasExpectedValue()
        => Assert.That(Flags.RainAltDone, Is.EqualTo("rain_alt_done"));

    [Test]
    public void AllAltDialogsDone_HasExpectedValue()
        => Assert.That(Flags.AllAltDialogsDone, Is.EqualTo("all_alt_dialogs_done"));

    [Test]
    public void NpcKrioraPurchased_HasExpectedValue()
        => Assert.That(Flags.NpcKrioraPurchased, Is.EqualTo("npc_kriora_purchased"));

    // ── Constants match signal-event convention ───────────────────────────────
    // Timeline signal events use the format: [signal arg="flag:{value}"]
    // Values must be lowercase_snake_case so the Dialogic signal handler can
    // parse them with a simple string.Substring(5) call.

    [TestCase(Flags.MetShizu)]
    [TestCase(Flags.SeenNorthExitHint)]
    [TestCase(Flags.BrixHorseAppeared)]
    [TestCase(Flags.BhataFalafelAppeared)]
    [TestCase(Flags.KrioraCrystalsAppeared)]
    [TestCase(Flags.GusTransformedToFrog)]
    [TestCase(Flags.ShizuMusicAuraActive)]
    [TestCase(Flags.LilyAltDone)]
    [TestCase(Flags.RainAltDone)]
    [TestCase(Flags.AllAltDialogsDone)]
    [TestCase(Flags.NpcKrioraPurchased)]
    public void Constant_IsLowercaseSnakeCase(string value)
    {
        Assert.That(value, Is.EqualTo(value.ToLowerInvariant()),
            "Flag values must be lower-case (signal args are case-sensitive).");
        Assert.That(value, Does.Not.Contain(" "),
            "Flag values must use underscores, not spaces.");
    }

    // ── TalkedTo helper ───────────────────────────────────────────────────────

    [TestCase("shizu",  "talked_to_shizu")]
    [TestCase("foran",  "talked_to_foran")]
    [TestCase("elder",  "talked_to_elder")]
    public void TalkedTo_ReturnsExpectedKey(string npcId, string expected)
        => Assert.That(Flags.TalkedTo(npcId), Is.EqualTo(expected));

    [Test]
    public void TalkedTo_NeverReturnsNullOrEmpty()
        => Assert.That(Flags.TalkedTo("x"), Is.Not.Null.And.Not.Empty);

    [Test]
    public void TalkedTo_ResultIsLowercase()
    {
        string key = Flags.TalkedTo("MyNPC");
        Assert.That(key, Does.StartWith("talked_to_"),
            "TalkedTo prefix must be lowercase.");
    }
}
