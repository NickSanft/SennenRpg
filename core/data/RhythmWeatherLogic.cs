namespace SennenRpg.Core.Data;

/// <summary>
/// Pure static helpers that map <see cref="WeatherType"/> to rhythm-obstacle
/// visual and gameplay modifiers. No Godot runtime needed.
/// </summary>
public static class RhythmWeatherLogic
{
    /// <summary>Extra beats added to obstacle travel time (Snowy slows notes).</summary>
    public static int ExtraBeatsUntilArrival(WeatherType w) => w == WeatherType.Snowy ? 1 : 0;

    /// <summary>Vertical wobble amplitude in pixels (Stormy shakes notes).</summary>
    public static float NoteYWobbleAmplitude(WeatherType w) => w == WeatherType.Stormy ? 3f : 0f;

    /// <summary>Initial opacity when a note spawns (Foggy = invisible, fades in).</summary>
    public static float NoteSpawnOpacity(WeatherType w) => w == WeatherType.Foggy ? 0.0f : 1.0f;

    /// <summary>Whether obstacles cycle through rainbow hues (Aurora).</summary>
    public static bool UseRainbowShift(WeatherType w) => w == WeatherType.Aurora;

    /// <summary>Whether a lightning-flash effect can trigger (Stormy).</summary>
    public static bool CanLightningFlash(WeatherType w) => w == WeatherType.Stormy;
}
