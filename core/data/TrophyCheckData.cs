namespace SennenRpg.Core.Data;

/// <summary>
/// Snapshot of player progress used by <see cref="TrophyLogic"/> to evaluate
/// unlock conditions. Pure DTO — no Godot dependencies.
/// </summary>
public sealed record TrophyCheckData
{
    public int TotalKills { get; init; }
    public int TotalMealsCooked { get; init; }
    public int MaxComboStreak { get; init; }
    public int Gold { get; init; }
    public int ItemCount { get; init; }
    public int DiscoveredEnemies { get; init; }
    public int TotalRecipes { get; init; }
    public int PerfectMeals { get; init; }
    public int MapsVisited { get; init; }
    public int NightBattles { get; init; }
    public int ClassesAtLevel5 { get; init; }
    public int MaxClassLevel { get; init; }
    public int PartySize { get; init; }
    public int JunkGoldSold { get; init; }
    public bool ReachedFloor3 { get; init; }
    public bool HasSRank { get; init; }
    public int TotalEnemyTypes { get; init; }
    public int TotalRecipeCount { get; init; }
    public int TotalMapCount { get; init; }
    public int TotalPartyMembers { get; init; }
}
