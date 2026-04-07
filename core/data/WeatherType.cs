namespace SennenRpg.Core.Data;

/// <summary>
/// Weather states driven by <see cref="WeatherLogic"/> and the <c>WeatherManager</c> autoload.
/// Aurora is a rare 5th state that stacks a stat buff and rare-forage bias for one roll window.
/// </summary>
public enum WeatherType
{
    Sunny  = 0,
    Foggy  = 1,
    Stormy = 2,
    Snowy  = 3,
    Aurora = 4,
}
