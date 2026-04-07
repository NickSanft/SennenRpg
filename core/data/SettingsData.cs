using System.Collections.Generic;

namespace SennenRpg.Core.Data;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum TextSize           { Small, Medium, Large }
public enum ColorblindMode     { Normal, Protanopia, Deuteranopia, Tritanopia }
public enum BattleDifficulty   { Easy, Normal, Hard }
public enum EncounterRateMode  { Normal, Low, Off }
public enum RhythmTimingWindow { Tight, Normal, Forgiving, AutoHit }
public enum BattleTextSpeed    { Slow, Normal, Fast, Instant }
public enum WindowScale        { Scale1x, Scale2x, Scale3x, Scale4x, Fullscreen }

/// <summary>
/// Immutable record holding all player settings.
/// No Godot dependency — serialised to/from user://settings.json by SettingsManager.
/// </summary>
public record SettingsData
{
    // ── Audio ──────────────────────────────────────────────────────────
    public float MasterVolume       { get; init; } = 1.0f;
    public float BgmVolume          { get; init; } = 0.8f;
    public float SfxVolume          { get; init; } = 1.0f;
    public float DialogTypingVolume { get; init; } = 0.6f;

    // ── Display ────────────────────────────────────────────────────────
    public TextSize       TextSize           { get; init; } = TextSize.Medium;
    public bool           HighContrastMode   { get; init; } = false;
    public ColorblindMode ColorblindMode     { get; init; } = ColorblindMode.Normal;
    public bool           ScreenFlashEffects { get; init; } = true;
    public WindowScale    WindowScale        { get; init; } = WindowScale.Scale2x;
    public bool           Fullscreen         { get; init; } = false;
    public bool           VSync              { get; init; } = true;
    public bool           ShowFps            { get; init; } = false;

    // ── Gameplay ───────────────────────────────────────────────────────
    public BattleDifficulty   BattleDifficulty   { get; init; } = BattleDifficulty.Normal;
    public EncounterRateMode  EncounterRateMode  { get; init; } = EncounterRateMode.Normal;
    public RhythmTimingWindow RhythmTimingWindow { get; init; } = RhythmTimingWindow.Normal;
    public BattleTextSpeed    BattleTextSpeed    { get; init; } = BattleTextSpeed.Normal;
    public bool               AutoAdvanceDialog  { get; init; } = false;

    /// <summary>
    /// When true, foraging triggers the rhythm minigame for grade-based bonuses.
    /// When false, foraging falls back to the legacy instant-grant of one default-table item.
    /// </summary>
    public bool               ForageMinigameEnabled { get; init; } = true;

    /// <summary>
    /// When true, the weather system ticks and swaps BGM/overlay on the world map.
    /// When false, WeatherManager stays pinned to Sunny with no overlay — used for
    /// accessibility, motion sensitivity, and low-perf builds.
    /// </summary>
    public bool               WeatherEnabled        { get; init; } = true;

    // ── Subtitles / Dialog ─────────────────────────────────────────────
    public bool AlwaysShowSpeakerName { get; init; } = true;
    public bool DialogHistoryEnabled  { get; init; } = true;

    // ── Controls ──────────────────────────────────────────────────────
    /// <summary>Action name → Godot Key enum integer. Empty = use project defaults.</summary>
    public Dictionary<string, int> KeyBindings { get; init; } = new();
}
