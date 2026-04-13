using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static settings formulas — fully NUnit-testable (GodotSharp types are fine in tests).
/// </summary>
public static class SettingsLogic
{
    // ── Audio ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a linear 0–1 slider value to decibels for AudioStreamPlayer.VolumeDb.
    /// Returns -80 dB (effectively silent) when linearVolume is 0.
    /// </summary>
    public static float LinearToDb(float linearVolume)
    {
        if (linearVolume <= 0f) return -80f;
        return MathF.Max(-80f, 20f * MathF.Log10(linearVolume));
    }

    // ── Difficulty ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the multiplier applied to both enemy HP and enemy damage output.
    /// Easy = 60 %, Hard = 150 %.
    /// </summary>
    public static float EnemyDifficultyMultiplier(BattleDifficulty d) => d switch
    {
        BattleDifficulty.Easy => 0.60f,
        BattleDifficulty.Hard => 1.50f,
        _                     => 1.00f,
    };

    // ── Encounter rate ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns a 0–1 multiplier applied on top of EncounterLogic.EncounterRate.
    /// Off = 0 (no encounters), Low = 30 % of normal rate.
    /// </summary>
    public static float EncounterRateMultiplier(EncounterRateMode m) => m switch
    {
        EncounterRateMode.Low => 0.30f,
        EncounterRateMode.Off => 0.00f,
        _                     => 1.00f,
    };

    // ── Rhythm windows ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the Good hit-window half-width in pixels for RhythmArena.
    /// AutoHit returns float.MaxValue so all notes are always in range.
    /// </summary>
    public static float RhythmGoodWindowPx(RhythmTimingWindow w) => w switch
    {
        RhythmTimingWindow.Tight     => 12f,
        RhythmTimingWindow.Forgiving => 40f,
        RhythmTimingWindow.AutoHit   => float.MaxValue,
        _                            => 22f,
    };

    /// <summary>Returns the Perfect hit-window half-width in pixels.</summary>
    public static float RhythmPerfectWindowPx(RhythmTimingWindow w) => w switch
    {
        RhythmTimingWindow.Tight     => 5f,
        RhythmTimingWindow.Forgiving => 18f,
        RhythmTimingWindow.AutoHit   => float.MaxValue,
        _                            => 9f,
    };

    /// <summary>
    /// Returns a scale factor applied to RhythmConstants timing windows.
    /// > 1 = forgiving (wider), &lt; 1 = tight (narrower), MaxValue = AutoHit.
    /// </summary>
    public static float RhythmTimingScale(RhythmTimingWindow w) => w switch
    {
        RhythmTimingWindow.Tight     => 0.55f,
        RhythmTimingWindow.Forgiving => 1.82f,
        RhythmTimingWindow.AutoHit   => float.MaxValue,
        _                            => 1.00f,
    };

    // ── Text & dialog ──────────────────────────────────────────────────────

    /// <summary>Returns the base font size in pixels for the given TextSize setting.</summary>
    public static int FontSizePx(TextSize size) => size switch
    {
        TextSize.Small => 12,
        TextSize.Large => 22,
        _              => 16,
    };

    /// <summary>
    /// Returns the Dialogic text speed in characters per second.
    /// 0 = instant (no typing animation).
    /// </summary>
    public static float DialogTextSpeed(BattleTextSpeed speed) => speed switch
    {
        BattleTextSpeed.Slow    => 20f,
        BattleTextSpeed.Fast    => 80f,
        BattleTextSpeed.Instant => 0f,
        _                       => 40f,
    };

    // ── High contrast ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the label outline pixel size for the given high-contrast state.
    /// 0 when disabled (no outline), 2 when enabled.
    /// </summary>
    public static int HighContrastOutlineSize(bool enabled) => enabled ? 2 : 0;

    // ── Colorblind palette ─────────────────────────────────────────────────

    /// <summary>Returns the HP-bar fill colour for the chosen colorblind mode.</summary>
    public static Color HpBarColor(ColorblindMode mode) => mode switch
    {
        ColorblindMode.Protanopia   => new Color(0.00f, 0.81f, 0.81f), // Cyan
        ColorblindMode.Deuteranopia => new Color(0.90f, 0.62f, 0.00f), // Orange
        ColorblindMode.Tritanopia   => new Color(0.80f, 0.00f, 0.00f), // Red
        _                           => new Color(1.00f, 1.00f, 0.00f), // Yellow (Normal)
    };

    /// <summary>Returns the MP-bar fill colour for the chosen colorblind mode.</summary>
    public static Color MpBarColor(ColorblindMode mode) => mode switch
    {
        ColorblindMode.Tritanopia => new Color(0.00f, 0.70f, 0.00f), // Green
        _                          => new Color(0.25f, 0.45f, 1.00f), // Blue (Normal)
    };

    // ── Window ─────────────────────────────────────────────────────────────

    /// <summary>Base game resolution (viewport size).</summary>
    public static readonly Vector2I BaseResolution = new(1280, 720);

    /// <summary>Returns the window size in pixels for the given scale.</summary>
    public static Vector2I WindowSize(WindowScale scale) => scale switch
    {
        WindowScale.Scale1x       => new Vector2I(640, 360),
        WindowScale.Scale2x       => new Vector2I(1280, 720),
        WindowScale.Scale3x       => new Vector2I(1600, 900),
        WindowScale.Scale4x       => new Vector2I(1920, 1080),
        WindowScale.Scale1440p    => new Vector2I(2560, 1440),
        WindowScale.ScaleUltrawide => new Vector2I(3440, 1440),
        WindowScale.Fullscreen    => new Vector2I(1920, 1080),
        _ => new Vector2I(1280, 720),
    };

    /// <summary>Display name for each window scale option.</summary>
    public static string WindowScaleLabel(WindowScale scale) => scale switch
    {
        WindowScale.Scale1x       => "640x360 (Small)",
        WindowScale.Scale2x       => "1280x720 (HD)",
        WindowScale.Scale3x       => "1600x900 (HD+)",
        WindowScale.Scale4x       => "1920x1080 (Full HD)",
        WindowScale.Scale1440p    => "2560x1440 (QHD)",
        WindowScale.ScaleUltrawide => "3440x1440 (Ultrawide)",
        WindowScale.Fullscreen    => "Fullscreen",
        _ => "1280x720 (HD)",
    };

    // ── Input key display ──────────────────────────────────────────────────

    /// <summary>
    /// Returns the effective <see cref="Key"/> for display purposes.
    /// Project actions defined with physical_keycode have <paramref name="keycode"/> = Key.None;
    /// in that case we fall back to <paramref name="physicalKeycode"/>.
    /// </summary>
    public static Key EffectiveKey(Key keycode, Key physicalKeycode)
        => keycode != Key.None ? keycode : physicalKeycode;
}
