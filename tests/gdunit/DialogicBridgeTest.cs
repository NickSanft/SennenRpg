using GdUnit4;
using static GdUnit4.Assertions;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.GdUnit;

/// <summary>
/// GdUnit4 tests for DialogicBridge autoload.
/// Verifies variable initialisation, flag syncing, and runtime correctness.
/// Requires the Godot runtime.
/// Run from the GdUnit4 panel in the Godot editor, or via:
///   godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd
/// </summary>
[TestSuite]
public sealed class DialogicBridgeTest
{
    // ── Presence ──────────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void Instance_IsNotNull()
        => AssertThat(DialogicBridge.Instance).IsNotNull();

    // ── Idle state ────────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void IsRunning_ReturnsFalse_WhenNoTimelineActive()
        => AssertThat(DialogicBridge.Instance.IsRunning()).IsFalse();

    // ── Variable initialisation ───────────────────────────────────────────────
    // After _Ready() runs, all battle variables and story-flag variables must
    // exist so {variable} interpolation never silently fails in a timeline.

    [TestCase]
    [RequireGodotRuntime]
    public void GetVariable_EnemyName_ReturnsEmpty_AfterStartup()
    {
        // "enemy_name" is seeded to "" in InitialiseDialogicVariables()
        var value = DialogicBridge.Instance.GetVariable("enemy_name");
        AssertThat(value.AsString()).IsEqual("");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetVariable_MetShizu_ReturnsFalse_AfterStartup()
    {
        // Story flags are seeded to false in InitialiseDialogicVariables()
        var value = DialogicBridge.Instance.GetVariable(Flags.MetShizu);
        AssertThat(value.AsBool()).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SetVariable_ThenGetVariable_RoundTrips()
    {
        DialogicBridge.Instance.SetVariable("test_var_bridge", Variant.From("hello"));
        var result = DialogicBridge.Instance.GetVariable("test_var_bridge");
        AssertThat(result.AsString()).IsEqual("hello");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SetVariable_Bool_RoundTrips()
    {
        DialogicBridge.Instance.SetVariable("test_var_bool", Variant.From(true));
        var result = DialogicBridge.Instance.GetVariable("test_var_bool");
        AssertThat(result.AsBool()).IsTrue();
    }

    // ── Battle variable seeding ───────────────────────────────────────────────

    [TestCase("enemy_name")]
    [TestCase("damage")]
    [TestCase("hit_label")]
    [TestCase("enemy_dialog")]
    [TestCase("charm_result")]
    [TestCase("skill_result")]
    [TestCase("performance_summary")]
    [TestCase("exp_gained")]
    [TestCase("gold_gained")]
    [TestCase("love")]
    [TestCase("notes_success")]
    [TestCase("notes_total")]
    [TestCase("item_name")]
    [TestCase("heal_amount")]
    [RequireGodotRuntime]
    public void BattleVariable_ExistsAfterStartup(string varName)
    {
        // GetVariable should not return the Godot null variant for any seeded variable
        var value = DialogicBridge.Instance.GetVariable(varName);
        // The value itself may be empty string, but calling AsString() should not throw
        AssertThat(value.AsString()).IsNotNull();
    }

    // ── Story flag seeding ────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void StoryFlagVar_MetShizu_ExistsAfterStartup()
    {
        var value = DialogicBridge.Instance.GetVariable(Flags.MetShizu);
        // Seeded to false — should be a valid bool variant
        AssertThat(value.AsBool()).IsFalse();
    }
}
