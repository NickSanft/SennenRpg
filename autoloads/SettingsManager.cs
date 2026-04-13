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
        AccessibilityOverlay.Instance?.Apply(Current.ColorblindMode, Current.HighContrastMode);
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
        ApplyWindowScale(s);
        // Full-screen shader: affects all sprites, tiles, and HUDs
        AccessibilityOverlay.Instance?.Apply(s.ColorblindMode, s.HighContrastMode);
    }

    private void ApplyWindowScale(SettingsData s)
    {
        var window = GetWindow();
        if (window == null) return;

        if (s.WindowScale == WindowScale.Fullscreen || s.Fullscreen)
        {
            window.Mode = Window.ModeEnum.Fullscreen;
        }
        else
        {
            window.Mode = Window.ModeEnum.Windowed;
            window.Borderless = false;
            var size = SettingsLogic.WindowSize(s.WindowScale);
            window.Size = size;
            // Center the window on screen
            var screenSize = DisplayServer.ScreenGetSize();
            window.Position = (screenSize - size) / 2;
        }

        DisplayServer.WindowSetVsyncMode(s.VSync
            ? DisplayServer.VSyncMode.Enabled
            : DisplayServer.VSyncMode.Disabled);

        GD.Print($"[SettingsManager] Window scale applied: {s.WindowScale} → {window.Size}");
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

        // Update the global fallback so labels that DON'T have an explicit
        // AddThemeFontSizeOverride automatically pick up the new size.
        ThemeDB.FallbackFontSize = fontSize;

        // Also update the default theme's per-type font sizes (set by
        // UiTheme.ApplyGlobalTheme) so dynamically-created labels inherit
        // the setting's text size instead of the hardcoded 12px.
        var theme = GetTree().Root.Theme;
        if (theme != null)
        {
            foreach (var type in new[] { "Label", "Button", "RichTextLabel", "LineEdit",
                "TextEdit", "OptionButton", "CheckButton", "ItemList", "TabBar" })
            {
                theme.SetFontSize("font_size", type, fontSize);
            }
            theme.SetFontSize("normal_font_size", "RichTextLabel", fontSize);
        }

        // DO NOT walk all Label nodes and override their font_size — that
        // would destroy the per-label sizes set by menu BuildUI code (18px
        // titles, 10px item names, 8px descriptions, etc.).
    }

    // ── Colorblind mode ───────────────────────────────────────────────────

    private void ApplyColorblind(SettingsData s)
    {
        Color hpColor = SettingsLogic.HpBarColor(s.ColorblindMode);
        Color mpColor = SettingsLogic.MpBarColor(s.ColorblindMode);

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
        int    outlineSize  = SettingsLogic.HighContrastOutlineSize(s.HighContrastMode);
        Color  outlineColor = Colors.Black;

        WalkNodes(GetTree().Root, node =>
        {
            if (node is not Label label) return;

            if (label.LabelSettings == null)
            {
                if (outlineSize <= 0) return; // nothing to do

                // Create a LabelSettings that preserves the label's current effective
                // font size so we don't accidentally override theme-based sizing.
                // LabelSettings.FontSize defaults to 16 in Godot, which would replace
                // the label's theme font size — so we explicitly copy it.
                int currentSize = label.GetThemeFontSize("font_size");
                label.LabelSettings = new LabelSettings { FontSize = currentSize };
            }

            label.LabelSettings.OutlineSize  = outlineSize;
            label.LabelSettings.OutlineColor = outlineColor;
        });
    }

    // ── Key bindings ──────────────────────────────────────────────────────

    private static void ApplyKeyBindings(SettingsData s)
    {
        foreach (var (action, code) in s.KeyBindings)
        {
            if (!InputMap.HasAction(action)) continue;

            // Preserve existing events that aren't the same type we're replacing
            var existing = InputMap.ActionGetEvents(action);
            InputMap.ActionEraseEvents(action);

            if (code < 0)
            {
                // Negative code = joypad button (stored as -(buttonIndex + 1))
                int buttonIndex = -(code + 1);
                // Re-add all non-joypad events, then add the new joypad button
                foreach (var ev in existing)
                    if (ev is not InputEventJoypadButton)
                        InputMap.ActionAddEvent(action, ev);
                InputMap.ActionAddEvent(action, new InputEventJoypadButton { ButtonIndex = (JoyButton)buttonIndex });
            }
            else
            {
                // Positive code = keyboard keycode
                // Re-add all non-keyboard events, then add the new key
                foreach (var ev in existing)
                    if (ev is not InputEventKey)
                        InputMap.ActionAddEvent(action, ev);
                InputMap.ActionAddEvent(action, new InputEventKey { Keycode = (Key)code });
            }
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
