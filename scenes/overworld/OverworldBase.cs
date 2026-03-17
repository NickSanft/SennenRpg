using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Overworld;

public partial class OverworldBase : Node2D
{
	[Export] public string MapId { get; set; } = "";
	[Export] public string BgmPath { get; set; } = "";

	protected Node2D YSort = null!;
	protected TileMapLayer? GroundLayer;
	protected TileMapLayer? WallsLayer;
	protected TileMapLayer? ObjectsLayer;
	protected Dictionary<string, Vector2> SpawnPoints { get; } = new();
	protected Vector2 DefaultSpawnPosition { get; set; } = Vector2.Zero;

	private CharacterBody2D? _player;

	public override void _Ready()
	{
		YSort = GetNode<Node2D>("YSort");

		// Cache TileMapLayer references (may be null if a map omits a layer)
		GroundLayer  = GetNodeOrNull<TileMapLayer>("Ground");
		WallsLayer   = GetNodeOrNull<TileMapLayer>("Walls");
		ObjectsLayer = GetNodeOrNull<TileMapLayer>("Objects");

		GameManager.Instance.SetLastMap(SceneFilePath);
		GameManager.Instance.SetState(GameState.Overworld);

		// Spawn player into YSort so it Y-sorts with NPCs
		var playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
		_player = playerScene.Instantiate<CharacterBody2D>();
		YSort.AddChild(_player);
		_player.GlobalPosition = GetSpawnPosition();

		if (!string.IsNullOrEmpty(BgmPath))
			AudioManager.Instance.PlayBgm(BgmPath);

		const string hudPath   = "res://scenes/hud/GameHud.tscn";
		if (ResourceLoader.Exists(hudPath))
			AddChild(GD.Load<PackedScene>(hudPath).Instantiate());

		const string pausePath = "res://scenes/menus/PauseMenu.tscn";
		if (ResourceLoader.Exists(pausePath))
			AddChild(GD.Load<PackedScene>(pausePath).Instantiate());

		GD.Print($"[OverworldBase] Ready. Map: {MapId}, Player spawned at {_player.GlobalPosition}");
	}

	/// <summary>
	/// Returns the spawn position for the player. Checks GameManager.LastSpawnId against
	/// registered SpawnPoints, then falls back to DefaultSpawnPosition.
	/// Subclasses register spawn points in their _Ready() before calling base._Ready().
	/// </summary>
	protected virtual Vector2 GetSpawnPosition()
	{
		string spawnId = GameManager.Instance.LastSpawnId;
		if (!string.IsNullOrEmpty(spawnId) && SpawnPoints.TryGetValue(spawnId, out var pos))
		{
			GameManager.Instance.SetLastSpawn(""); // consume — don't reuse on next load
			return pos;
		}
		return DefaultSpawnPosition;
	}
}
