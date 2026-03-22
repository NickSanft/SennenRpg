using Godot;
using Godot.Collections;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Hud;

namespace SennenRpg.Scenes.Overworld;

public partial class OverworldBase : Node2D
{
	[Export] public string MapId   { get; set; } = "";
	[Export] public string BgmPath { get; set; } = "";

	/// <summary>
	/// Encounters rolled during overworld movement. Each entry is checked independently
	/// using its EncounterChancePerStep (0–100, percentage per step tile).
	/// Leave empty to disable random encounters on this map.
	/// </summary>
	[Export] public Array<EncounterData> RandomEncounterTable { get; set; } = [];

	protected Node2D YSort = null!;
	protected TileMapLayer? GroundLayer;
	protected TileMapLayer? WallsLayer;
	protected TileMapLayer? ObjectsLayer;
	protected Godot.Collections.Dictionary<string, Vector2> SpawnPoints { get; } = new();
	protected Vector2 DefaultSpawnPosition { get; set; } = Vector2.Zero;

	private CharacterBody2D? _player;
	private Vector2          _lastPlayerPos;
	private float            _stepAccumulator;
	private bool             _encounterLocked; // prevents overlapping battle transitions
	private Rect2            _worldBounds;

	private const float StepDistance = 32f; // pixels per "step" roll

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
		_lastPlayerPos = _player.GlobalPosition;

		// Register SpawnPoint nodes placed in the scene editor.
		// These override any positions registered in code with the same ID.
		foreach (var node in GetTree().GetNodesInGroup("spawn_points"))
		{
			if (node is SpawnPoint sp)
			{
				SpawnPoints[sp.SpawnId] = sp.GlobalPosition;
				if (sp.SpawnId == "default")
					DefaultSpawnPosition = sp.GlobalPosition;
				GD.Print($"[OverworldBase] SpawnPoint registered: '{sp.SpawnId}' @ {sp.GlobalPosition}");
			}
		}

		if (!string.IsNullOrEmpty(BgmPath))
			AudioManager.Instance.PlayBgm(BgmPath);

		ApplyCameraBoundsFromGround();
		PreloadNpcTimelines();

		const string hudPath   = "res://scenes/hud/GameHud.tscn";
		if (ResourceLoader.Exists(hudPath))
			AddChild(GD.Load<PackedScene>(hudPath).Instantiate());

		const string pausePath = "res://scenes/menus/PauseMenu.tscn";
		if (ResourceLoader.Exists(pausePath))
			AddChild(GD.Load<PackedScene>(pausePath).Instantiate());

		const string areaLabelPath = "res://scenes/hud/AreaNameLabel.tscn";
		if (!string.IsNullOrEmpty(MapId) && ResourceLoader.Exists(areaLabelPath))
		{
			var areaLabel = GD.Load<PackedScene>(areaLabelPath).Instantiate<AreaNameLabel>();
			AddChild(areaLabel);
			areaLabel.ShowAreaName(MapId);
		}

		var minimap = new SennenRpg.Scenes.Hud.MinimapHud();
		AddChild(minimap);
		minimap.Initialise(_worldBounds);

		AddChild(new SennenRpg.Scenes.Hud.DialogHistoryOverlay());

		GD.Print($"[OverworldBase] Ready. Map: {MapId}, Player spawned at {_player.GlobalPosition}");
	}

	public override void _Process(double delta)
	{
		if (_player == null || RandomEncounterTable.Count == 0 || _encounterLocked) return;
		if (GameManager.Instance.CurrentState != GameState.Overworld) return;

		float moved = _player.GlobalPosition.DistanceTo(_lastPlayerPos);
		if (moved > 0.5f)
		{
			_stepAccumulator += moved;
			_lastPlayerPos = _player.GlobalPosition;
		}

		while (_stepAccumulator >= StepDistance)
		{
			_stepAccumulator -= StepDistance;
			if (TryRandomEncounter()) return; // battle started — stop rolling
		}
	}

	// ── Helpers ───────────────────────────────────────────────────────

	private bool TryRandomEncounter()
	{
		int idx = (int)GD.RandRange(0, RandomEncounterTable.Count - 1);
		var enc = RandomEncounterTable[idx];
		if (enc == null) return false;

		float roll = (float)GD.RandRange(0.0, 100.0);
		if (roll >= enc.EncounterChancePerStep) return false;

		_encounterLocked = true;
		GD.Print($"[OverworldBase] Random encounter triggered (roll {roll:F1} < {enc.EncounterChancePerStep}).");
		_ = SceneTransition.Instance.ToBattleAsync(enc);
		return true;
	}

	/// <summary>
	/// Reads the Ground TileMapLayer's used rect and applies it as camera limits
	/// to the PhantomCamera2D inside the player scene.
	/// </summary>
	private void ApplyCameraBoundsFromGround()
	{
		if (_player == null) return;

		// Prefer the nested "Ground/Ground" layer which holds actual tiles;
		// fall back to the top-level Ground layer if the nested one is absent.
		var groundLayer = GetNodeOrNull<TileMapLayer>("Ground/Ground") ?? GroundLayer;
		if (groundLayer == null) return;

		var usedRect = groundLayer.GetUsedRect();
		if (!usedRect.HasArea()) return;

		var tileSize = groundLayer.TileSet?.TileSize ?? new Vector2I(16, 16);
		var origin   = groundLayer.GlobalPosition;

		int left   = (int)origin.X + usedRect.Position.X * tileSize.X;
		int top    = (int)origin.Y + usedRect.Position.Y * tileSize.Y;
		int right  = (int)origin.X + (usedRect.Position.X + usedRect.Size.X) * tileSize.X;
		int bottom = (int)origin.Y + (usedRect.Position.Y + usedRect.Size.Y) * tileSize.Y;

		_worldBounds = new Rect2(left, top, right - left, bottom - top);

		// Apply to PhantomCamera2D so it clamps movement to the map bounds.
		var pcam = _player.GetNodeOrNull("PhantomCamera2D");
		pcam?.Set("limit_left",   left);
		pcam?.Set("limit_top",    top);
		pcam?.Set("limit_right",  right);
		pcam?.Set("limit_bottom", bottom);

		GD.Print($"[OverworldBase] Camera bounds set: L={left} T={top} R={right} B={bottom}");
	}

	/// <summary>
	/// Kicks off background loading for all NPC timeline files on this map so dialog
	/// starts instantly the first time the player talks to an NPC.
	/// </summary>
	private void PreloadNpcTimelines()
	{
		int count = 0;
		foreach (var node in GetTree().GetNodesInGroup("interactable"))
		{
			if (node is not Npc npc) continue;

			void TryPreload(string path)
			{
				if (!string.IsNullOrEmpty(path) && ResourceLoader.Exists(path))
				{
					ResourceLoader.LoadThreadedRequest(path);
					count++;
				}
			}

			TryPreload(npc.TimelinePath);
			foreach (var path in npc.AltTimelinePaths)
				TryPreload(path);
		}
		if (count > 0)
			GD.Print($"[OverworldBase] Preloading {count} NPC timeline(s) in background.");
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
