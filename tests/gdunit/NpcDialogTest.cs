using GdUnit4;
using static GdUnit4.Assertions;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Overworld;

namespace SennenRpg.Tests.GdUnit;

/// <summary>
/// GdUnit4 tests for Npc.SelectTimeline and NpcDialogOption.
/// Requires the Godot runtime so that NpcDialogOption (a Resource subclass) can be instantiated.
/// Run from the GdUnit4 panel in the Godot editor, or via:
///   godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd
/// </summary>
[TestSuite]
public sealed class NpcDialogTest
{
    // ── No options ────────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void SelectTimeline_NoOptions_ReturnsDefault()
    {
        string result = Npc.SelectTimeline("default.dtl", [], _ => false);
        AssertThat(result).IsEqual("default.dtl");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SelectTimeline_EmptyDefaultPath_ReturnsEmpty()
    {
        string result = Npc.SelectTimeline("", [], _ => false);
        AssertThat(result).IsEqual("");
    }

    // ── Single option ─────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void SelectTimeline_FlagNotSet_ReturnsDefault()
    {
        var opts = Options(("some_flag", "alt.dtl"));
        string result = Npc.SelectTimeline("default.dtl", opts, _ => false);
        AssertThat(result).IsEqual("default.dtl");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SelectTimeline_FlagSet_ReturnsAlt()
    {
        var opts = Options(("some_flag", "alt.dtl"));
        string result = Npc.SelectTimeline("default.dtl", opts, flag => flag == "some_flag");
        AssertThat(result).IsEqual("alt.dtl");
    }

    // ── Multiple options ──────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void SelectTimeline_FirstMatchWins_WhenBothFlagsSet()
    {
        var opts = Options(("flag_a", "a.dtl"), ("flag_b", "b.dtl"));
        string result = Npc.SelectTimeline("default.dtl", opts, _ => true);
        AssertThat(result).IsEqual("a.dtl");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SelectTimeline_OnlySecondFlagSet_ReturnsSecondPath()
    {
        var opts = Options(("flag_a", "a.dtl"), ("flag_b", "b.dtl"));
        string result = Npc.SelectTimeline("default.dtl", opts, flag => flag == "flag_b");
        AssertThat(result).IsEqual("b.dtl");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SelectTimeline_NoFlagsSet_ReturnsDefault()
    {
        var opts = Options(("flag_a", "a.dtl"), ("flag_b", "b.dtl"));
        string result = Npc.SelectTimeline("default.dtl", opts, _ => false);
        AssertThat(result).IsEqual("default.dtl");
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void SelectTimeline_OptionWithEmptyFlag_IsSkipped()
    {
        // An option with no RequiredFlag set should never match
        var opts = Options(("", "skipped.dtl"));
        string result = Npc.SelectTimeline("default.dtl", opts, _ => true);
        AssertThat(result).IsEqual("default.dtl");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SelectTimeline_UsesFlagConstants()
    {
        // Verify that Flags.MetShizu works as a RequiredFlag value
        var opts = Options((Flags.MetShizu, "shizu_alt.dtl"));
        string result = Npc.SelectTimeline("shizu.dtl", opts, flag => flag == Flags.MetShizu);
        AssertThat(result).IsEqual("shizu_alt.dtl");
    }

    // ── NpcDialogOption properties ────────────────────────────────────────────

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

    // ── Helper ────────────────────────────────────────────────────────────────

    private static NpcDialogOption[] Options(params (string flag, string path)[] entries)
    {
        var opts = new NpcDialogOption[entries.Length];
        for (int i = 0; i < entries.Length; i++)
            opts[i] = new NpcDialogOption { RequiredFlag = entries[i].flag, TimelinePath = entries[i].path };
        return opts;
    }
}
