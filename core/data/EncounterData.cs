using Godot;

namespace SennenRpg.Core.Data;

public partial class EncounterData : Resource
{
    [Export] public EnemyData[] Enemies { get; set; } = [];
    [Export] public string BackgroundId { get; set; } = "default";
    [Export] public float EncounterChancePerStep { get; set; } = 1.0f;
}
