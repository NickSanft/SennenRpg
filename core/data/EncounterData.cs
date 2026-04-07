using Godot;
using Godot.Collections;

namespace SennenRpg.Core.Data;

[GlobalClass]
public partial class EncounterData : Resource
{
	[Export] public Array<EnemyData> Enemies { get; set; } = [];
	[Export] public string BackgroundId { get; set; } = "default";
	[Export] public float EncounterChancePerStep { get; set; } = 1.0f;
	/// <summary>Battle track BPM for this encounter. Overrides enemy BattleBpm if > 0.</summary>
	[Export] public float BattleBpm { get; set; } = 180f;
	/// <summary>Battle BGM path for this encounter. Leave empty to use enemy's BattleBgmPath.</summary>
    [Export] public string BattleBgmPath { get; set; } = "";

	/// <summary>
	/// Weather states under which this encounter is preferred (selection weight ×2).
	/// Leave empty for no preference. Values are <see cref="WeatherType"/> enum ints.
	/// Example: a slime encounter might list Stormy + Sunny so it's more common in
	/// rain but still possible in clear skies.
	/// </summary>
	[Export] public Array<int> PreferredWeather { get; set; } = [];
}
