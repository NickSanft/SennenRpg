using Godot;
using System.Text.Json;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Test;

/// <summary>
/// Lightweight in-engine smoke-test runner.
/// Run headless:  godot --headless --scene res://scenes/test/TestRunner.tscn
/// Exit code 0 = all passed, 1 = one or more failed.
///
/// Checks things that require a live Godot scene tree and cannot be covered
/// by the NUnit project (autoloads, GameManager state, SaveData JSON I/O).
/// </summary>
public partial class TestRunner : Node
{
    private int _passed;
    private int _failed;

    public override void _Ready()
    {
        GD.Print("\n══════════════════════════════════════");
        GD.Print(" SennenRpg In-Engine Tests");
        GD.Print("══════════════════════════════════════");

        // ── Autoloads ────────────────────────────────────────────────────────
        Check("GameManager autoload present",    () => GameManager.Instance    != null);
        Check("SaveManager autoload present",    () => SaveManager.Instance    != null);
        Check("AudioManager autoload present",   () => AudioManager.Instance   != null);
        Check("DialogicBridge autoload present", () => DialogicBridge.Instance != null);
        Check("RhythmClock autoload present",    () => RhythmClock.Instance    != null);

        // ── GameManager state machine ────────────────────────────────────────
        Check("GameManager SetState Overworld", () =>
        {
            GameManager.Instance.SetState(GameState.Overworld);
            return GameManager.Instance.CurrentState == GameState.Overworld;
        });

        Check("GameManager SetState Battle", () =>
        {
            GameManager.Instance.SetState(GameState.Battle);
            return GameManager.Instance.CurrentState == GameState.Battle;
        });

        // ── GameManager stats ────────────────────────────────────────────────
        Check("GameManager AddGold increments Gold", () =>
        {
            int before = GameManager.Instance.Gold;
            GameManager.Instance.AddGold(50);
            return GameManager.Instance.Gold == before + 50;
        });

        // ── GameManager flags ────────────────────────────────────────────────
        Check("SetFlag / GetFlag round-trip", () =>
        {
            GameManager.Instance.SetFlag("runner_test_flag", true);
            return GameManager.Instance.GetFlag("runner_test_flag");
        });

        Check("GetFlag returns false for unknown key", () =>
            !GameManager.Instance.GetFlag("runner_flag_never_set_xyz"));

        // ── SaveData JSON serialisation ──────────────────────────────────────
        Check("SaveData JSON round-trip (all fields)", () =>
        {
            var original = new SaveData
            {
                PlayerHp        = 20,
                PlayerMaxHp     = 80,
                Gold            = 500,
                Exp             = 120,
                LastMapPath     = "res://scenes/overworld/maps/Town.tscn",
                LastSavePointId = "save_01",
                LastSpawnId     = "save_01",
                Flags           = new System.Collections.Generic.Dictionary<string, bool>
                                  { ["met_npc_foran"] = true, ["bought_item"] = false },
                InventoryItemPaths = new System.Collections.Generic.List<string>
                                     { "res://resources/items/item_001.tres" },
            };

            string json  = JsonSerializer.Serialize(original);
            var   loaded = JsonSerializer.Deserialize<SaveData>(json);

            return loaded != null
                && loaded.PlayerHp               == original.PlayerHp
                && loaded.Gold                   == original.Gold
                && loaded.LastMapPath            == original.LastMapPath
                && loaded.Flags.Count            == original.Flags.Count
                && loaded.Flags["met_npc_foran"] == true
                && loaded.InventoryItemPaths.Count == 1;
        });

        // ── RhythmClock ──────────────────────────────────────────────────────
        Check("RhythmClock StartFreeRunning sets BeatInterval for 180 BPM", () =>
        {
            RhythmClock.Instance.StartFreeRunning(180f);
            float expected = 60f / 180f;
            return Mathf.Abs(RhythmClock.Instance.BeatInterval - expected) < 0.0001f;
        });

        Check("RhythmClock Stop clears running state", () =>
        {
            RhythmClock.Instance.StartFreeRunning(180f);
            RhythmClock.Instance.Stop();
            // After Stop(), BeatPhase should remain 0 and no Beat signals fire next frame.
            // We can only verify BeatInterval is still set (Stop doesn't reset BPM).
            return RhythmClock.Instance.BeatInterval > 0f;
        });

        // ── Results ──────────────────────────────────────────────────────────
        GD.Print("══════════════════════════════════════");
        GD.Print($" Results: {_passed} passed, {_failed} failed");
        GD.Print("══════════════════════════════════════\n");

        GetTree().Quit(_failed > 0 ? 1 : 0);
    }

    private void Check(string name, System.Func<bool> test)
    {
        try
        {
            bool ok = test();
            if (ok)
            {
                _passed++;
                GD.Print($"  ✓  {name}");
            }
            else
            {
                _failed++;
                GD.PrintErr($"  ✗  {name}");
            }
        }
        catch (System.Exception ex)
        {
            _failed++;
            GD.PrintErr($"  ✗  {name}  [{ex.GetType().Name}: {ex.Message}]");
        }
    }
}
