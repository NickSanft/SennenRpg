namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static settings formulas — no Godot dependency, fully NUnit-testable.
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
}
