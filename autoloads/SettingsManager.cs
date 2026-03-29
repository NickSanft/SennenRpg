using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

/// <summary>
/// Autoload that owns all player settings.
/// Loads from user://settings.json on startup and saves whenever Apply() is called.
/// Call Apply(newSettings) to change settings; all integration points update immediately.
/// </summary>
public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; } = null!;

    private const string SavePath = "user://settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented      = true,
        Converters         = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public SettingsData Current { get; private set; } = new();

    public override void _Ready()
    {
        Instance    = this;
        ProcessMode = ProcessModeEnum.Always;
        Current     = Load();
        ApplyAll(Current);
    }

    /// <summary>
    /// Replaces the current settings, applies all changes immediately, and persists to disk.
    /// </summary>
    public void Apply(SettingsData next)
    {
        Current = next;
        ApplyAll(next);
        Save();
    }

    /// <summary>
    /// Re-applies visual settings (text size, colorblind, high contrast) to the current scene tree.
    /// Call this from a scene's _Ready() to pick up settings that were saved before the scene loaded.
    /// </summary>
    public void ApplyVisuals()
    {
        ApplyTextSize(Current);
        ApplyColorblind(Current);
        ApplyHighContrast(Current);
    }

    // ── Persistence ───────────────────────────────────────────────────────

    private static SettingsData Load()
    {
        string path = ProjectSettings.GlobalizePath(SavePath);
        if (!File.Exists(path)) return new SettingsData();
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SettingsData>(json, JsonOptions) ?? new SettingsData();
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[SettingsManager] Could not load settings: {ex.Message}. Using defaults.");
            return new SettingsData();
        }
    }

    private void Save()
    {
        string path = ProjectSettings.GlobalizePath(SavePath);
        try
        {
            string json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(path, json);
            GD.Print("[SettingsManager] Settings saved.");
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[SettingsManager] Could not save settings: {ex.Message}");
        }
    }

    // ── Apply orchestration ───────────────────────────────────────────────

    private void ApplyAll(SettingsData s)
    {
        ApplyAudio(s);
        ApplyKeyBindings(s);
        ApplyDialogSettings(s);
        ApplyTextSize(s);
        ApplyColorblind(s);
        ApplyHighContrast(s);
    }

    // ── Audio ─────────────────────────────────────────────────────────────

    private static void ApplyAudio(SettingsData s)
    {
        AudioManager.Instance?.SetVolumes(s.MasterVolume, s.BgmVolume, s.SfxVolume, s.DialogTypingVolume);
    }

    // ── Text size ─────────────────────────────────────────────────────────

    private void ApplyTextSize(SettingsData s)
    {
        int fontSize = SettingsLogic.FontSizePx(s.TextSize);
        WalkNodes(GetTree().Root, node =>
        {
            if (node is Label label)
                label.AddThemeFontSizeOverride("font_size", fontSize);
        });
    }

    // ── Colorblind mode ───────────────────────────────────────────────────

    private void ApplyColorblind(SettingsData s)
    {
        Color hpColor = s.ColorblindMode switch
        {
            ColorblindMode.Protanopia   => new Color(0.00f, 0.81f, 0.81f), // Cyan
            ColorblindMode.Deuteranopia => new Color(0.90f, 0.62f, 0.00f), // Orange
            ColorblindMode.Tritanopia   => new Color(0.80f, 0.00f, 0.00f), // Red
            _                           => new Color(1.00f, 1.00f, 0.00f), // Yellow (normal)
        };
        Color mpColor = s.ColorblindMode switch
        {
            ColorblindMode.Tritanopia => new Color(0.00f, 0.70f, 0.00f),   // Green
            _                         => new Color(0.25f, 0.45f, 1.00f),   // Blue (normal)
        };

        WalkNodes(GetTree().Root, node =>
        {
            if (node is not ColorRect rect) return;
            if (node.Name == "HpBar") rect.Color = hpColor;
            if (node.Name == "MpBar") rect.Color = mpColor;
        });
    }

    // ── High contrast ─────────────────────────────────────────────────────

    private void ApplyHighContrast(SettingsData s)
    {
        int outlineSize = s.HighContrastMode ? 4 : 2;
        WalkNodes(GetTree().Root, node =>
        {
            if (node is Label { LabelSettings: { } ls })
                ls.OutlineSize = outlineSize;
        });
    }

    // ── Key bindings ──────────────────────────────────────────────────────

    private static void ApplyKeyBindings(SettingsData s)
    {
        foreach (var (action, keycode) in s.KeyBindings)
        {
            if (!InputMap.HasAction(action)) continue;
            InputMap.ActionEraseEvents(action);
            InputMap.ActionAddEvent(action, new InputEventKey { Keycode = (Key)keycode });
        }
    }

    // ── Dialog settings ───────────────────────────────────────────────────

    private static void ApplyDialogSettings(SettingsData s)
    {
        DialogicBridge.Instance?.ApplyTextSettings(
            SettingsLogic.DialogTextSpeed(s.BattleTextSpeed),
            s.AutoAdvanceDialog);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static void WalkNodes(Node root, Action<Node> visitor)
    {
        visitor(root);
        foreach (Node child in root.GetChildren())
            WalkNodes(child, visitor);
    }
}
