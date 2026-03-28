namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static day/night cycle logic — no Godot dependency, NUnit-testable.
///
/// A cycle flip (day ↔ night) occurs every <see cref="CycleLengthTiles"/> tiles walked
/// on the world map.
/// </summary>
public static class DayNightLogic
{
    /// <summary>Number of world-map tiles that constitute one half-cycle (day or night).</summary>
    public const int CycleLengthTiles = 20;

    /// <summary>
    /// Returns true when the cumulative tile count crosses a cycle boundary.
    /// Fires at step 20, 40, 60, … — never at 0.
    /// </summary>
    public static bool ShouldFlip(int tilesWalked)
        => tilesWalked > 0 && tilesWalked % CycleLengthTiles == 0;

    /// <summary>Returns the new day/night state after a flip.</summary>
    public static bool ApplyFlip(bool currentlyNight) => !currentlyNight;
}
