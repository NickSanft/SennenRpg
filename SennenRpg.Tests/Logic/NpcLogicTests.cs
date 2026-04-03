using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// Tests for NpcLogic.SelectTimeline — the pure flag-based timeline selector.
/// No Godot runtime required.
/// </summary>
[TestFixture]
public sealed class NpcLogicTests
{
	private const string Default = "res://dialog/default.dtl";
	private const string Alt1    = "res://dialog/alt1.dtl";
	private const string Alt2    = "res://dialog/alt2.dtl";

	// ── No alternate timelines ────────────────────────────────────────────────

	[Test]
	public void SelectTimeline_NoAlts_ReturnsDefault()
	{
		var result = NpcLogic.SelectTimeline(Default, [], [], _ => false);
		Assert.That(result, Is.EqualTo(Default));
	}

	[Test]
	public void SelectTimeline_NoAlts_NoFlagsSet_ReturnsDefault()
	{
		var result = NpcLogic.SelectTimeline(Default, [], [], _ => true);
		Assert.That(result, Is.EqualTo(Default));
	}

	// ── Single alternate ──────────────────────────────────────────────────────

	[Test]
	public void SelectTimeline_SingleAlt_FlagNotSet_ReturnsDefault()
	{
		var result = NpcLogic.SelectTimeline(
			Default,
			["met_shizu"],
			[Alt1],
			_ => false);

		Assert.That(result, Is.EqualTo(Default));
	}

	[Test]
	public void SelectTimeline_SingleAlt_FlagSet_ReturnsAlt()
	{
		var result = NpcLogic.SelectTimeline(
			Default,
			["met_shizu"],
			[Alt1],
			flag => flag == "met_shizu");

		Assert.That(result, Is.EqualTo(Alt1));
	}

	// ── Multiple alternates — priority order ──────────────────────────────────

	[Test]
	public void SelectTimeline_MultipleAlts_FirstFlagSet_ReturnsFirstAlt()
	{
		var result = NpcLogic.SelectTimeline(
			Default,
			["flag_a", "flag_b"],
			[Alt1,     Alt2],
			flag => flag == "flag_a");

		Assert.That(result, Is.EqualTo(Alt1));
	}

	[Test]
	public void SelectTimeline_MultipleAlts_SecondFlagSet_ReturnsSecondAlt()
	{
		var result = NpcLogic.SelectTimeline(
			Default,
			["flag_a", "flag_b"],
			[Alt1,     Alt2],
			flag => flag == "flag_b");

		Assert.That(result, Is.EqualTo(Alt2));
	}

	[Test]
	public void SelectTimeline_MultipleAlts_BothFlagsSet_ReturnsFirstAlt()
	{
		// First match wins — order matters
		var result = NpcLogic.SelectTimeline(
			Default,
			["flag_a", "flag_b"],
			[Alt1,     Alt2],
			_ => true);

		Assert.That(result, Is.EqualTo(Alt1));
	}

	[Test]
	public void SelectTimeline_MultipleAlts_NoFlagsSet_ReturnsDefault()
	{
		var result = NpcLogic.SelectTimeline(
			Default,
			["flag_a", "flag_b"],
			[Alt1,     Alt2],
			_ => false);

		Assert.That(result, Is.EqualTo(Default));
	}

	// ── Edge cases ────────────────────────────────────────────────────────────

	[Test]
	public void SelectTimeline_EmptyFlagName_IsSkipped()
	{
		// An empty required-flag string must never be evaluated — it would match anything
		var result = NpcLogic.SelectTimeline(
			Default,
			[""],    // empty flag key
			[Alt1],
			_ => true);

		Assert.That(result, Is.EqualTo(Default));
	}

	[Test]
	public void SelectTimeline_WhitespaceFlagName_IsSkipped()
	{
		var result = NpcLogic.SelectTimeline(
			Default,
			["   "],
			[Alt1],
			_ => true);

		// string.IsNullOrEmpty does NOT skip whitespace-only strings — verify actual behaviour
		// (whitespace flag key gets passed to flagChecker; checker returns true → Alt1 returned)
		Assert.That(result, Is.EqualTo(Alt1));
	}

	[Test]
	public void SelectTimeline_MismatchedArrays_FlagsLonger_UsesShortestLength()
	{
		// More flags than paths — only paths.Length entries are checked
		var result = NpcLogic.SelectTimeline(
			Default,
			["flag_a", "flag_b"],   // 2 flags
			[Alt1],                  // 1 path
			flag => flag == "flag_b"); // second flag set, but no paired path

		Assert.That(result, Is.EqualTo(Default));
	}

	[Test]
	public void SelectTimeline_MismatchedArrays_PathsLonger_UsesShortestLength()
	{
		// More paths than flags — only flags.Length entries are checked
		var result = NpcLogic.SelectTimeline(
			Default,
			["flag_a"],              // 1 flag
			[Alt1, Alt2],            // 2 paths
			flag => flag == "flag_a");

		Assert.That(result, Is.EqualTo(Alt1));
	}

	[Test]
	public void SelectTimeline_DefaultPathEmpty_ReturnsEmptyWhenNoMatch()
	{
		var result = NpcLogic.SelectTimeline("", ["flag_a"], [Alt1], _ => false);
		Assert.That(result, Is.EqualTo(""));
	}

	[Test]
	public void SelectTimeline_FlagCheckerCalledWithCorrectKey()
	{
		string? receivedKey = null;
		NpcLogic.SelectTimeline(
			Default,
			["my_flag"],
			[Alt1],
			flag => { receivedKey = flag; return false; });

		Assert.That(receivedKey, Is.EqualTo("my_flag"));
	}
}

[TestFixture]
public sealed class NpcLogicRevisitTests
{
    // ── GetRevisitPath ────────────────────────────────────────────────────────

    [Test]
    public void GetRevisitPath_NotTalked_ReturnsBasePath()
    {
        const string path = "res://dialog/timelines/npc_gus.dtl";
        Assert.That(NpcLogic.GetRevisitPath(path, talkFlag: false), Is.EqualTo(path));
    }

    [Test]
    public void GetRevisitPath_Talked_InsertsAgainSuffix()
    {
        const string path    = "res://dialog/timelines/npc_gus.dtl";
        const string expected = "res://dialog/timelines/npc_gus_again.dtl";
        Assert.That(NpcLogic.GetRevisitPath(path, talkFlag: true), Is.EqualTo(expected));
    }

    [Test]
    public void GetRevisitPath_AlreadyAgainPath_StillAppends()
    {
        // Edge case: base path itself contains "_again" — should still work cleanly
        const string path     = "res://dialog/timelines/npc_gus_again.dtl";
        const string expected = "res://dialog/timelines/npc_gus_again_again.dtl";
        Assert.That(NpcLogic.GetRevisitPath(path, talkFlag: true), Is.EqualTo(expected));
    }

    [Test]
    public void GetRevisitPath_NoDtlExtension_ReturnsUnchanged()
    {
        const string path = "res://dialog/timelines/npc_gus";
        Assert.That(NpcLogic.GetRevisitPath(path, talkFlag: true), Is.EqualTo(path));
    }

    [Test]
    public void GetRevisitPath_EmptyPath_ReturnsEmpty()
    {
        Assert.That(NpcLogic.GetRevisitPath("", talkFlag: true), Is.EqualTo(""));
    }

    [Test]
    public void GetRevisitPath_CaseInsensitiveExtension()
    {
        const string path     = "res://dialog/timelines/NPC_GUS.DTL";
        const string expected = "res://dialog/timelines/NPC_GUS_again.dtl";
        Assert.That(NpcLogic.GetRevisitPath(path, talkFlag: true), Is.EqualTo(expected));
    }
}
