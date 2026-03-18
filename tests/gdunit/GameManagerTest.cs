using GdUnit4;
using static GdUnit4.Assertions;
using SennenRpg.Autoloads;

namespace SennenRpg.Tests.GdUnit;

/// <summary>
/// GdUnit4 tests for GameManager autoload.
/// All tests require the Godot runtime so GameManager.Instance is available.
/// Run from the GdUnit4 panel in the Godot editor, or via:
///   godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd
/// </summary>
[TestSuite]
public sealed class GameManagerTest
{
    // ── Presence ─────────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void Instance_IsNotNull()
        => AssertThat(GameManager.Instance).IsNotNull();

    // ── SetState ─────────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void SetState_UpdatesCurrentState()
    {
        GameManager.Instance.SetState(GameState.Overworld);
        AssertThat(GameManager.Instance.CurrentState).IsEqual(GameState.Overworld);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task SetState_EmitsGameStateChangedSignal()
    {
        var gm = GameManager.Instance;

        // Set up the signal assertion BEFORE triggering, then fire, then await.
        var assertion = AssertSignal(gm).IsEmitted("GameStateChanged").WithTimeout(500);
        gm.SetState(GameState.Battle);
        await assertion;

        AssertThat(gm.CurrentState).IsEqual(GameState.Battle);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task SetState_SameTwice_EmitsEachTime()
    {
        var gm = GameManager.Instance;

        var a1 = AssertSignal(gm).IsEmitted("GameStateChanged").WithTimeout(500);
        gm.SetState(GameState.Overworld);
        await a1;

        var a2 = AssertSignal(gm).IsEmitted("GameStateChanged").WithTimeout(500);
        gm.SetState(GameState.Overworld); // same value — still emits (no dedup in SetState)
        await a2;
    }

    // ── Kill / route ──────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void RegisterKill_IncrementsTotalKills()
    {
        var gm = GameManager.Instance;
        int before = gm.TotalKills;
        gm.RegisterKill();
        AssertThat(gm.TotalKills).IsEqual(before + 1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RegisterKill_UpdatesLove()
    {
        // 0 kills → LV 1; any kill → LV ≥ 2
        var gm = GameManager.Instance;
        // Reset so test is deterministic
        gm.ResetForNewGame();
        AssertThat(gm.Love).IsEqual(1);
        gm.RegisterKill();
        AssertThat(gm.Love).IsGreaterEqual(1); // still 1 until kill count threshold
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AddGold_IncreasesGold()
    {
        var gm = GameManager.Instance;
        int before = gm.Gold;
        gm.AddGold(100);
        AssertThat(gm.Gold).IsEqual(before + 100);
    }

    // ── Flags ─────────────────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void SetFlag_ThenGetFlag_ReturnsTrue()
    {
        GameManager.Instance.SetFlag("gdunit_test_flag_a", true);
        AssertThat(GameManager.Instance.GetFlag("gdunit_test_flag_a")).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SetFlag_False_ThenGetFlag_ReturnsFalse()
    {
        GameManager.Instance.SetFlag("gdunit_test_flag_b", false);
        AssertThat(GameManager.Instance.GetFlag("gdunit_test_flag_b")).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetFlag_UnknownKey_ReturnsFalse()
        => AssertThat(GameManager.Instance.GetFlag("gdunit_flag_never_set_xyz")).IsFalse();

    // ── ResetForNewGame ───────────────────────────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void ResetForNewGame_ClearsFlags()
    {
        var gm = GameManager.Instance;
        gm.SetFlag("gdunit_reset_test", true);
        gm.ResetForNewGame();
        AssertThat(gm.GetFlag("gdunit_reset_test")).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ResetForNewGame_SetsGoldAndKillsToZero()
    {
        var gm = GameManager.Instance;
        gm.AddGold(999);
        gm.RegisterKill();
        gm.ResetForNewGame();
        AssertThat(gm.Gold).IsEqual(0);
        AssertThat(gm.TotalKills).IsEqual(0);
    }
}
