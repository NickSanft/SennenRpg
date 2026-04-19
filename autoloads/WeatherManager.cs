using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Autoloads;

/// <summary>
/// Autoload that owns the current weather state. Overworld scenes call
/// <see cref="TickStep"/> each step; every <see cref="RollInterval"/> steps the manager
/// rolls for a new state via <see cref="WeatherLogic.RollNext"/> and emits
/// <see cref="WeatherChanged"/>.
///
/// Dungeon scenes should set <see cref="Locked"/> = true in _Ready and clear it in
/// _ExitTree so the manager doesn't swap BGM underneath them.
/// </summary>
public partial class WeatherManager : Node
{
    public static WeatherManager Instance { get; private set; } = null!;

    [Signal] public delegate void WeatherChangedEventHandler(int newWeatherInt);

    /// <summary>Current weather state. Persists through save/load.</summary>
    public WeatherType Current { get; private set; } = WeatherType.Sunny;

    /// <summary>Step counter used by <see cref="WeatherLogic.ShouldRoll"/>.</summary>
    public int StepCounter { get; private set; }

    /// <summary>Configurable steps-per-roll. Defaults to <see cref="WeatherLogic.DefaultRollInterval"/>.</summary>
    public int RollInterval { get; set; } = WeatherLogic.DefaultRollInterval;

    /// <summary>
    /// When true, <see cref="TickStep"/> is a no-op. Dungeon scenes and cutscenes
    /// should set this so weather doesn't advance or swap music behind them.
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Cached "sunny" BGM path for the currently active map. Whichever scene is
    /// currently showing should set this in _Ready so the weather system knows
    /// what track to restore when it rolls back to Sunny.
    /// </summary>
    public string SunnyBgmPath { get; set; } = "";

    public override void _Ready()
    {
        Instance    = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// Advance the step counter by one. When the counter hits <see cref="RollInterval"/>,
    /// roll for a new weather state and emit <see cref="WeatherChanged"/> if it changed.
    /// No-op when <see cref="Locked"/>.
    /// </summary>
    public void TickStep()
    {
        if (Locked) return;

        // Accessibility / perf setting: when weather is disabled, hold Sunny.
        // If we were already mid-another-weather when the setting flipped, snap
        // back to Sunny so the overlay clears on the next scene load.
        if (SettingsManager.Instance != null && !SettingsManager.Instance.Current.WeatherEnabled)
        {
            if (Current != WeatherType.Sunny)
                ForceSet(WeatherType.Sunny);
            return;
        }

        StepCounter++;
        if (!WeatherLogic.ShouldRoll(StepCounter, RollInterval)) return;

        double roll = GD.RandRange(0.0, 1.0);
        var next = WeatherLogic.RollNext(Current, roll, WeatherTransitionTable.Default);

        if (next != Current)
        {
            Current = next;
            GD.Print($"[WeatherManager] Weather changed → {WeatherLogic.DisplayName(next)} (step {StepCounter}).");
            EmitSignal(SignalName.WeatherChanged, (int)next);

            // First time the player sees weather roll to something other than Sunny.
            if (next != WeatherType.Sunny)
                TutorialManager.Instance?.Trigger(TutorialIds.WeatherFirst);
        }
    }

    /// <summary>
    /// Manually set the weather state (used by debug tools, cutscenes, and the
    /// save loader). Always fires <see cref="WeatherChanged"/> even if the state
    /// didn't change so listeners can re-apply visuals on load.
    /// </summary>
    public void ForceSet(WeatherType weather)
    {
        Current = weather;
        GD.Print($"[WeatherManager] Force-set weather → {WeatherLogic.DisplayName(weather)}.");
        EmitSignal(SignalName.WeatherChanged, (int)weather);
    }

    /// <summary>
    /// Resolve the BGM path to play under the current weather, given the map's
    /// sunny-track fallback. Uses <see cref="SunnyBgmPath"/> if no explicit path
    /// is passed.
    /// </summary>
    public string GetBgmForCurrentWeather(string? sunnyFallback = null)
    {
        string sunny = !string.IsNullOrEmpty(sunnyFallback) ? sunnyFallback : SunnyBgmPath;
        return WeatherLogic.BgmPathFor(Current, sunny);
    }

    // ── Save / Load ───────────────────────────────────────────────────────

    /// <summary>Populate fields from a loaded save. Emits WeatherChanged so visuals re-apply.</summary>
    public void LoadFromSave(WeatherType current, int stepCounter)
    {
        Current     = current;
        StepCounter = stepCounter;
        EmitSignal(SignalName.WeatherChanged, (int)Current);
    }

    /// <summary>Reset to default state for a new game.</summary>
    public void Reset()
    {
        Current     = WeatherType.Sunny;
        StepCounter = 0;
        Locked      = false;
        SunnyBgmPath = "";
    }
}
