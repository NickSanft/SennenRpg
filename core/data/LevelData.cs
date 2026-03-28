namespace SennenRpg.Core.Data;

/// <summary>
/// Pure level-up math. No Godot dependency — fully unit-testable via NUnit.
///
/// ExpThreshold(level): cumulative EXP needed to reach that level.
/// Formula: level² × 20
///   Lv 2 =   80 EXP  (gap  80)
///   Lv 3 =  180 EXP  (gap 100)
///   Lv 4 =  320 EXP  (gap 140)
///   Lv 5 =  500 EXP  (gap 180)
/// </summary>
public static class LevelData
{
    public const int MaxLevel = 99;

    /// <summary>Total cumulative EXP required to reach <paramref name="level"/>.</summary>
    public static int ExpThreshold(int level) => level * level * 20;

    /// <summary>
    /// Returns how many levels are gained given the player's current total EXP and current level.
    /// Supports multi-level gains from a single large EXP award.
    /// </summary>
    public static int CheckLevelUp(int currentExp, int currentLevel)
    {
        if (currentLevel >= MaxLevel) return 0;
        int gained = 0;
        while (currentLevel + gained < MaxLevel
               && currentExp >= ExpThreshold(currentLevel + gained + 1))
            gained++;
        return gained;
    }
}
