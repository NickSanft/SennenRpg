using Godot;
using Godot.Collections;

namespace SennenRpg.Core.Data;

[GlobalClass]
public partial class EncounterData : Resource
{
	[Export] public Array<EnemyData> Enemies { get; set; } = [];
	[Export] public string BackgroundId { get; set; } = "default";
	[Export] public float EncounterChancePerStep { get; set; } = 1.0f;
}
