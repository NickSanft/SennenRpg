using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// Per-stat growth rates for Fire Emblem-style level ups.
/// Each field is a percentage (0–100): the chance that stat increases by 1 on level up.
/// Place the player's instance at res://resources/characters/player_growth_rates.tres.
/// </summary>
[GlobalClass]
public partial class GrowthRates : Resource
{
    [Export] public int MaxHp      { get; set; } = 60;
    [Export] public int Attack     { get; set; } = 45;
    [Export] public int Defense    { get; set; } = 35;
    [Export] public int Speed      { get; set; } = 50;
    [Export] public int Magic      { get; set; } = 40;
    [Export] public int Resistance { get; set; } = 30;
    [Export] public int Luck       { get; set; } = 55;
}
