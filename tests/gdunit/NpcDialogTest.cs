using GdUnit4;
using static GdUnit4.Assertions;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Overworld;

namespace SennenRpg.Tests.GdUnit;

/// <summary>
/// Tests for Npc.SelectTimeline (pure C# logic, no Godot runtime required)
/// and NpcDialogOption property round-trips (requires Godot runtime for Resource).
/// Run from the GdUnit4 panel in the Godot editor, or via:
///   godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd
/// </summary>
[TestSuite]
public sealed class NpcDialogTest
{
    // ── No options ────────────────────────────────────────────────────────────

    [TestCase]
    public void SelectTimeline_NoOptions_ReturnsDefault()
    {
        string result = Npc.SelectTimeline("default.dtl", [], [], _ => false);
        AssertThat(result).IsEqual("default.dtl");
    }

    [TestCase]
    public void SelectTimeline_EmptyDefaultPath_ReturnsEmpty()
    {
        string result = Npc.SelectTimeline("", [], [], _ => false);
        AssertThat(result).IsEqual("");
    }

    // ── Single option ─────────────────────────────────────────────────────────

    [TestCase]
    public void SelectTimeline_FlagNotSet_ReturnsDefault()
    {
        string result = Npc.SelectTimeline("default.dtl", ["some_flag"], ["alt.dtl"], _ => false);
        AssertThat(result).IsEqual("default.dtl");
    }

    [TestCase]
    public void SelectTimeline_FlagSet_ReturnsAlt()
    {
        string result = Npc.SelectTimeline("default.dtl", ["some_flag"], ["alt.dtl"], flag => flag == "some_flag");
        AssertThat(result).IsEqual("alt.dtl");
    }

    // ── Multiple options ──────────────────────────────────────────────────────

    [TestCase]
    public void SelectTimeline_FirstMatchWins_WhenBothFlagsSet()
    {
        string result = Npc.SelectTimeline("default.dtl", ["flag_a", "flag_b"], ["a.dtl", "b.dtl"], _ => true);
        AssertThat(result).IsEqual("a.dtl");
    }

    [TestCase]
    public void SelectTimeline_OnlySecondFlagSet_ReturnsSecondPath()
    {
        string result = Npc.SelectTimeline("default.dtl", ["flag_a", "flag_b"], ["a.dtl", "b.dtl"], flag => flag == "flag_b");
        AssertThat(result).IsEqual("b.dtl");
    }

    [TestCase]
    public void SelectTimeline_NoFlagsSet_ReturnsDefault()
    {
        string result = Npc.SelectTimeline("default.dtl", ["flag_a", "flag_b"], ["a.dtl", "b.dtl"], _ => false);
        AssertThat(result).IsEqual("default.dtl");
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [TestCase]
    public void SelectTimeline_OptionWithEmptyFlag_IsSkipped()
    {
        // An entry with an empty flag should never match
        string result = Npc.SelectTimeline("default.dtl", [""], ["skipped.dtl"], _ => true);
        AssertThat(result).IsEqual("default.dtl");
    }

    [TestCase]
    public void SelectTimeline_UnequalArrayLengths_UsesShortestCount()
    {
        // Extra paths beyond the flags array are ignored
        string result = Npc.SelectTimeline("default.dtl", ["flag_a"], ["a.dtl", "extra.dtl"], flag => flag == "flag_a");
        AssertThat(result).IsEqual("a.dtl");
    }

    [TestCase]
    public void SelectTimeline_UsesFlagConstants()
    {
        string result = Npc.SelectTimeline("shizu.dtl", [Flags.MetShizu], ["shizu_alt.dtl"], flag => flag == Flags.MetShizu);
        AssertThat(result).IsEqual("shizu_alt.dtl");
    }

    // ── NpcDialogOption properties ────────────────────────────────────────────
    // NpcDialogOption is a Godot Resource subclass — requires the Godot runtime.

    [TestCase]
    [RequireGodotRuntime]
    public void NpcDialogOption_DefaultsAreEmpty()
    {
        var opt = new NpcDialogOption();
        AssertThat(opt.RequiredFlag).IsEqual("");
        AssertThat(opt.TimelinePath).IsEqual("");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void NpcDialogOption_PropertiesRoundTrip()
    {
        var opt = new NpcDialogOption
        {
            RequiredFlag = Flags.MetShizu,
            TimelinePath = "res://dialog/timelines/shizu.dtl",
        };
        AssertThat(opt.RequiredFlag).IsEqual(Flags.MetShizu);
        AssertThat(opt.TimelinePath).IsEqual("res://dialog/timelines/shizu.dtl");
    }
}
