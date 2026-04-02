using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Dungeon Floor 2 — middle level.
/// Both stairs use MapExit nodes (AutoTrigger = true) set up in the scene.
/// StairsUp returns to Floor 1, StairsDown descends to Floor 3.
/// </summary>
public partial class DungeonFloor2 : OverworldBase
{
	public override bool CountsForTownRewards => true;

	public override void _Ready()
	{
		const string enc = "res://resources/encounters/encounter_003.tres";
		if (ResourceLoader.Exists(enc))
		{
			var encounter = GD.Load<EncounterData>(enc);
			encounter.EncounterChancePerStep = 15f; // 15% per step on floor 2
			RandomEncounterTable.Add(encounter);
		}

		base._Ready();
	}
}
