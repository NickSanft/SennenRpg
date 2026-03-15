using Godot;

namespace SennenRpg.Core.Data;

public partial class EnemyData : Resource
{
    [Export] public string EnemyId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string FlavorText { get; set; } = "";
    [Export] public CharacterStats? Stats { get; set; }
    [Export] public Texture2D? BattleSprite { get; set; }
    [Export] public string[] ActOptions { get; set; } = [];
    [Export] public bool CanBeSpared { get; set; } = false;
    [Export] public int GoldDrop { get; set; } = 0;
    [Export] public int ExpDrop { get; set; } = 0;
    [Export] public PackedScene? AttackPatternScene { get; set; }
}
