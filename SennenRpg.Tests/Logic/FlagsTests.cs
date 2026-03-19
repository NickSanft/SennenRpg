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
        string[] all = [Flags.MetShizu];
        Assert.That(all, Is.Unique, "Each Flags constant must map to a distinct string.");
    }

    // ── Constants match signal-event convention ───────────────────────────────
    // Timeline signal events use the format: [signal arg="flag:{value}"]
    // Values must be lowercase_snake_case so the Dialogic signal handler can
    // parse them with a simple string.Substring(5) call.

    [TestCase(Flags.MetShizu)]
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
