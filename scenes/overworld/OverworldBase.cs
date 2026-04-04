using Godot;
using Godot.Collections;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Hud;
using SennenRpg.Scenes.Player;

namespace SennenRpg.Scenes.Overworld;

public partial class OverworldBase : Node2D
{
	[Export] public string MapId        { get; set; } = "";
	[Export] public string BgmPath      { get; set; } = "";

	/// <summary>
	/// When true, spawns DungeonPlayer (16×16 Sen_Overworld sprite, grid-locked movement)
	/// instead of the default Player (32×32, free movement). Set on dungeon floor scenes.
	/// </summary>
	[Export] public bool UseSmallPlayer { get; set; } = false;

	/// <summary>
	/// Encounters rolled during overworld movement. Each entry is checked independently
	/// using its EncounterChancePerStep (0–100, percentage per step tile).
	/// Leave empty to disable random encounters on this map.
	/// </summary>
	[Export] public Array<EncounterData> RandomEncounterTable { get; set; } = [];

	/// <summary>
	/// When true, each step on this map ticks the Mellyr Outpost passive reward counters
	/// (Rain gold, Lily forge items). Set to true on 16×16 dungeon floor maps.
	/// </summary>
	public virtual bool CountsForTownRewards => false;

	protected Node2D YSort = null!;
	protected TileMapLayer? GroundLayer;
	protected TileMapLayer? WallsLayer;
	protected TileMapLayer? ObjectsLayer;
	protected Godot.Collections.Dictionary<string, Vector2> SpawnPoints { get; } = new();
	protected Vector2 DefaultSpawnPosition { get; set; } = Vector2.Zero;

	private CharacterBody2D? _player;
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

		// Register SpawnPoint nodes from the scene BEFORE spawning the player,
		// so GetSpawnPosition() can find them.
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

		// Spawn player into YSort so it Y-sorts with NPCs
		string playerPath = UseSmallPlayer
			? "res://scenes/player/DungeonPlayer.tscn"
			: "res://scenes/player/Player.tscn";
		var playerScene = GD.Load<PackedScene>(playerPath);
		_player = playerScene.Instantiate<CharacterBody2D>();
		YSort.AddChild(_player);
		_player.GlobalPosition = GetSpawnPosition();

		// Connect Moved signal — works for both Player and DungeonPlayer
		if (_player is Player.Player p)
			p.Moved += OnPlayerMoved;
		else if (_player is Player.DungeonPlayer dp)
			dp.Moved += OnPlayerMoved;

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

	/// <summary>
	/// Signal-driven step accumulation. Called by Player.Moved / DungeonPlayer.Moved
	/// whenever the character moves, replacing per-frame position polling.
	/// </summary>
	private void OnPlayerMoved(float distance)
	{
		if (GameManager.Instance.CurrentState != GameState.Overworld) return;

		_stepAccumulator += distance;

		while (_stepAccumulator >= StepDistance)
		{
			_stepAccumulator -= StepDistance;

			if (CountsForTownRewards)
				TickTownRewards();

			if (RandomEncounterTable.Count > 0 && !_encounterLocked)
				if (TryRandomEncounter()) return;
		}
	}

	private void TickTownRewards()
	{
		var gm = GameManager.Instance;
		var result = TownRewardLogic.TryTick(
			gm.TownStepCounter,
			gm.GetFlag(Flags.NpcRainPurchased),
			gm.PendingRainGold,
			gm.GetFlag(Flags.NpcLilyPurchased),
			gm.PendingLilyRecipes.Count,
			gm.PlayerLevel);

		gm.TownStepCounter = result.NewCounter;
		gm.PendingRainGold = result.NewPendingRainGold;
		if (result.LilyRecipe != null)
			gm.PendingLilyRecipes.Add(result.LilyRecipe);
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
		// Save player position so they return here after battle, not at the map's default spawn
		if (_player != null)
			GameManager.Instance.BattleReturnPosition = _player.GlobalPosition;
		// Sample ground tile color for battle background gradient
		SampleTileColorForBattle();

		GD.Print($"[OverworldBase] Random encounter triggered (roll {roll:F1} < {enc.EncounterChancePerStep}).");
		_ = SceneTransition.Instance.ToBattleAsync(enc);
		return true;
	}

	private void SampleTileColorForBattle()
	{
		if (_player == null) return;
		var groundLayer = GetNodeOrNull<TileMapLayer>("Ground/Ground")
			?? GetNodeOrNull<TileMapLayer>("Ground");
		if (groundLayer?.TileSet == null) return;

		var tilePos = groundLayer.LocalToMap(groundLayer.ToLocal(_player.GlobalPosition));
		int srcId   = groundLayer.GetCellSourceId(tilePos);
		if (srcId < 0) return;

		var atlasCoords = groundLayer.GetCellAtlasCoords(tilePos);
		var source      = groundLayer.TileSet.GetSource(srcId) as TileSetAtlasSource;
		var texture     = source?.Texture;
		if (texture == null) return;

		var img = texture.GetImage();
		if (img == null) return;

		// Sample center pixel of the tile
		int tileSize = groundLayer.TileSet.TileSize.X;
		int px = atlasCoords.X * tileSize + tileSize / 2;
		int py = atlasCoords.Y * tileSize + tileSize / 2;
		if (px < img.GetWidth() && py < img.GetHeight())
		{
			var color = img.GetPixel(px, py);
			BattleRegistry.Instance.PendingBackgroundColor = color;
		}
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
		// Return to exact position after battle (consumed on use)
		if (GameManager.Instance.BattleReturnPosition is { } battlePos)
		{
			GameManager.Instance.BattleReturnPosition = null;
			return battlePos;
		}

		string spawnId = GameManager.Instance.LastSpawnId;
		if (!string.IsNullOrEmpty(spawnId) && SpawnPoints.TryGetValue(spawnId, out var pos))
		{
			GameManager.Instance.SetLastSpawn(""); // consume — don't reuse on next load
			return pos;
		}
		return DefaultSpawnPosition;
	}
}
