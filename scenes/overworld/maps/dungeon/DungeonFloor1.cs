using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Dungeon Floor 1 — shallowest level.
/// StairsDown exits to DungeonFloor2. The surface exit (StairsUp) is handled
/// manually here because returning to the WorldMap requires setting
/// GameManager.WorldMapReturnTile before transitioning.
/// </summary>
public partial class DungeonFloor1 : OverworldBase
{
	/// <summary>
	/// World-map tile the player returns to when leaving the dungeon via the surface exit.
	/// Must match the ReturnTile set on the WorldMapEntrance node in WorldMap.tscn.
	/// </summary>
	[Export] public Vector2I SurfaceReturnTile { get; set; } = new Vector2I(8, 12);

	public override bool CountsForTownRewards => true;

	private Area2D _stairsUp = null!;

	public override void _Ready()
	{
		// Populate random encounters before base._Ready() connects the step handler
		const string enc = "res://resources/encounters/encounter_001.tres";
		if (ResourceLoader.Exists(enc))
		{
			var encounter = GD.Load<EncounterData>(enc);
			encounter.EncounterChancePerStep = 12f; // 12% per step on floor 1
			RandomEncounterTable.Add(encounter);
		}

		base._Ready();
		if (Engine.IsEditorHint()) return;

		_stairsUp = GetNode<Area2D>("StairsUp");
		_stairsUp.BodyEntered += OnSurfaceExitEntered;

		if (!GameManager.Instance.GetFlag(Flags.DungeonDiscovered))
			GameManager.Instance.SetFlag(Flags.DungeonDiscovered, true);

		// Re-entering the dungeon resets the boss state — the Centiphantom Quing
		// rises again and the Floor 3 warp becomes inert until it is felled anew.
		if (GameManager.Instance.GetFlag(Flags.DungeonBossDefeated))
			GameManager.Instance.SetFlag(Flags.DungeonBossDefeated, false);
	}

	private void OnSurfaceExitEntered(Node2D body)
	{
		if (!body.IsInGroup("player")) return;
		GameManager.Instance.WorldMapReturnTile = SurfaceReturnTile;
		_ = SceneTransition.Instance.GoToAsync("res://scenes/overworld/WorldMap.tscn", autoSave: true);
	}
}
