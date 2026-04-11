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
	/// When true, <see cref="WeatherManager"/> is locked for the duration of this scene
	/// (no step ticks, no BGM swaps). Defaults to true since OverworldBase powers
	/// interior maps and dungeon floors, which should be weather-free.
	/// The open WorldMap sets it false implicitly by not inheriting from OverworldBase.
	/// </summary>
	[Export] public bool LockWeather { get; set; } = true;

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

	// Phase 4 — overworld follower chain. Only populated on 16×16 sprite maps
	// (UseSmallPlayer == true). Each entry is positioned 1 tile back further than the prior.
	private FollowerTrail? _followerTrail;
	private System.Collections.Generic.List<SennenRpg.Scenes.Player.PartyFollower> _followers = new();
	// Tracks the leader's position from BEFORE the latest step. Pushing this into the
	// trail (instead of the leader's current position) means stepsBack=1 returns the
	// tile directly behind the leader, not the leader's own tile.
	private Vector2 _lastLeaderPos;
	/// <summary>True iff this scene actually subscribed to PartyOrderChanged. Prevents
	/// double-unsubscribe in _ExitTree on scenes that never subscribed in the first place.</summary>
	private bool _subscribedToPartyOrder;

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

		// Dungeons and interior maps opt out of the weather system by default.
		if (LockWeather && WeatherManager.Instance != null)
			WeatherManager.Instance.Locked = true;

		// Register SpawnPoint nodes from the scene BEFORE spawning the player,
		// so GetSpawnPosition() can find them.
		SpawnPoint? firstRegistered = null;
		foreach (var node in GetTree().GetNodesInGroup("spawn_points"))
		{
			if (node is SpawnPoint sp)
			{
				SpawnPoints[sp.SpawnId] = sp.GlobalPosition;
				if (sp.SpawnId == "default")
					DefaultSpawnPosition = sp.GlobalPosition;
				if (firstRegistered == null)
					firstRegistered = sp;
				GD.Print($"[OverworldBase] SpawnPoint registered: '{sp.SpawnId}' @ {sp.GlobalPosition}");
			}
		}

		// Fallback: if no spawn point claimed the "default" id, use the first registered
		// one. Without this the player lands at world (0, 0) on maps like DungeonFloor1
		// (whose spawn points are "entrance" / "stairs_down") whenever the entry
		// transition didn't set GameManager.LastSpawnId — including the WorldMap →
		// dungeon transition, which uses WorldMapEntrance instead of MapExit.
		if (DefaultSpawnPosition == Vector2.Zero && firstRegistered != null)
		{
			DefaultSpawnPosition = firstRegistered.GlobalPosition;
			GD.Print($"[OverworldBase] No 'default' spawn point — using '{firstRegistered.SpawnId}' as fallback @ {firstRegistered.GlobalPosition}");
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

		// Phase 4: spawn party followers on 16×16 sprite maps only.
		// Towns / interiors that use the 32×32 player sprite skip this entirely.
		if (UseSmallPlayer)
		{
			ApplyLeaderSpriteToPlayer();
			SpawnFollowers();
			var gm = GameManager.Instance;
			if (gm != null)
			{
				gm.PartyOrderChanged += OnPartyOrderChanged;
				_subscribedToPartyOrder = true;
			}
		}

		if (!string.IsNullOrEmpty(BgmPath))
			AudioManager.Instance.PlayBgm(BgmPath);

		ApplyCameraBoundsFromGround();
		FillUnpaintedTilesAsWalls();
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
		if (GameManager.Instance == null) return;
		if (GameManager.Instance.CurrentState != GameState.Overworld) return;

		// Phase 4: push the leader's PREVIOUS position into the trail so a follower at
		// stepsBack=1 lands one tile behind the leader. We track _lastLeaderPos locally
		// because the Moved signal fires AFTER the move completes — by the time we get
		// here _player.GlobalPosition is already the new tile centre.
		// Use IsInstanceValid to defend against the (rare) case where the player has
		// been queue-freed during a scene transition mid-step.
		if (_followerTrail != null && _player != null && IsInstanceValid(_player))
		{
			_followerTrail.Push(_lastLeaderPos);
			_lastLeaderPos = _player.GlobalPosition;
		}

		_stepAccumulator += distance;

		while (_stepAccumulator >= StepDistance)
		{
			_stepAccumulator -= StepDistance;

			if (CountsForTownRewards)
				TickTownRewards();

			if (RandomEncounterTable.Count > 0 && !_encounterLocked && !GameManager.Instance.DebugNoEncounters)
				if (TryRandomEncounter()) return;
		}
	}

	// ── Party followers (Phase 4) ────────────────────────────────────

	private void SpawnFollowers()
	{
		var party = GameManager.Instance.Party;
		if (party.IsEmpty || _player == null) return;

		// Skip the leader (Sen, by default at index 0) — only the *other* members follow.
		var followerMembers = new System.Collections.Generic.List<SennenRpg.Core.Data.PartyMember>();
		for (int i = 0; i < party.Members.Count; i++)
		{
			if (i == party.LeaderIndex) continue;
			followerMembers.Add(party.Members[i]);
		}
		if (followerMembers.Count == 0) return;

		// Capacity must hold at least one position per follower so the deepest one
		// has a real history entry to read once enough steps have been taken.
		_followerTrail = new FollowerTrail(capacity: System.Math.Max(8, followerMembers.Count + 2));

		// Snap the spawn to a 16-px tile centre. DungeonPlayer snaps itself on its
		// first _Process frame; we mirror that here so the followers (which don't run
		// the dungeon player's snap logic) end up on the same tile as Sen even if the
		// raw spawn-point position was off-grid.
		Vector2 raw   = _player.GlobalPosition;
		Vector2 spawn = SnapToTileCentre(raw);
		_player.GlobalPosition = spawn;
		_lastLeaderPos = spawn;

		// Make sure the leader visually outranks every follower so the player is never
		// covered up when they overlap (spawn frame, intersections at corners, etc.).
		_player.ZIndex = 10;

		for (int i = 0; i < followerMembers.Count; i++)
		{
			var member   = followerMembers[i];
			var follower = new SennenRpg.Scenes.Player.PartyFollower();
			follower.Configure(member.OverworldSpritePath, _followerTrail, stepsBack: i + 1, spawnPosition: spawn);
			follower.ZIndex = 5; // Below the leader, above the ground tiles.
			YSort.AddChild(follower);
			_followers.Add(follower);
		}

		GD.Print($"[OverworldBase] Spawned {_followers.Count} party follower(s).");
	}

	private void OnPartyOrderChanged()
	{
		if (!UseSmallPlayer) return;
		ApplyLeaderSpriteToPlayer();
		RespawnFollowers();
	}

	private void ApplyLeaderSpriteToPlayer()
	{
		var leader = GameManager.Instance.Party.Leader;
		if (leader == null || _player == null) return;
		string path = string.IsNullOrEmpty(leader.OverworldSpritePath)
			? "res://assets/sprites/player/Sen_Overworld.png"
			: leader.OverworldSpritePath;
		// Both the small and the dungeon player support SetSpriteSheet via overload.
		if (_player is Player.DungeonPlayer dp) dp.SetSpriteSheet(path);
	}

	private void RespawnFollowers()
	{
		foreach (var f in _followers)
		{
			if (IsInstanceValid(f)) f.QueueFree();
		}
		_followers.Clear();
		_followerTrail = null;
		SpawnFollowers();
	}

	/// <summary>
	/// Snap a world coordinate to the centre of its 16×16 tile, matching the snap
	/// rule used by <c>DungeonPlayer.SnapToGrid</c>. Centres land at (8, 8) within
	/// each tile.
	/// </summary>
	private static Vector2 SnapToTileCentre(Vector2 pos)
	{
		const int tile = 16;
		return new Vector2(
			Mathf.Floor(pos.X / tile) * tile + tile * 0.5f,
			Mathf.Floor(pos.Y / tile) * tile + tile * 0.5f);
	}

	public override void _ExitTree()
	{
		// Release the weather lock we took in _Ready so the next scene (e.g. WorldMap)
		// can resume advancing weather.
		if (LockWeather && WeatherManager.Instance != null)
			WeatherManager.Instance.Locked = false;

		// Unsubscribe from PartyOrderChanged ONLY if we actually subscribed in _Ready
		// (the subscribe is gated on UseSmallPlayer; an unconditional unsubscribe leaks
		// no handlers but can throw on certain teardown ordering — be exact).
		if (_subscribedToPartyOrder)
		{
			var gm = GameManager.Instance;
			if (gm != null) gm.PartyOrderChanged -= OnPartyOrderChanged;
			_subscribedToPartyOrder = false;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Keycode: Key.L })
		{
			var gm = GameManager.Instance;
			gm.DebugNoEncounters = !gm.DebugNoEncounters;
			GD.Print($"[Debug] Encounters {(gm.DebugNoEncounters ? "OFF" : "ON")}");
			GetViewport().SetInputAsHandled();
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
			gm.PlayerLevel,
			gm.GetFlag(Flags.NpcBhataPurchased),
			gm.PendingBhataAles,
			gm.GetFlag(Flags.NpcKrioraPurchased),
			gm.PendingKrioraRecipes.Count);

		gm.TownStepCounter  = result.NewCounter;
		gm.PendingRainGold  = result.NewPendingRainGold;
		gm.PendingBhataAles = result.NewPendingBhataAles;
		if (result.LilyRecipe != null)
			gm.PendingLilyRecipes.Add(result.LilyRecipe);
		if (result.KrioraRecipe != null)
			gm.PendingKrioraRecipes.Add(result.KrioraRecipe);
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
	/// For any tile within the Ground layer's bounding rect that has no ground tile
	/// painted, paint a wall tile on the Walls layer. This ensures empty space is impassable.
	/// Also adds a 1-tile wall border around the entire ground area.
	/// </summary>
	/// <summary>
	/// Collect all TileMapLayers that aren't the Walls layer — these represent ground/floor.
	/// Checks Ground, Ground/Ground, and any dynamically added layers (like MappTiles).
	/// </summary>
	private System.Collections.Generic.List<TileMapLayer> GetAllGroundLayers()
	{
		var layers = new System.Collections.Generic.List<TileMapLayer>();
		CollectTileMapLayers(this, layers, "Walls");
		return layers;
	}

	private static void CollectTileMapLayers(Node node, System.Collections.Generic.List<TileMapLayer> list, string excludeName)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is TileMapLayer tl && child.Name != excludeName)
				list.Add(tl);
			// Recurse into containers (like the Ground parent node)
			if (child is not TileMapLayer)
				CollectTileMapLayers(child, list, excludeName);
			else if (child.Name.ToString() == "Ground")
				CollectTileMapLayers(child, list, excludeName); // check nested Ground/Ground
		}
	}

	protected void FillUnpaintedTilesAsWalls()
	{
		var wallsLayer = GetNodeOrNull<TileMapLayer>("Walls");
		if (wallsLayer == null) return;

		// Ensure the Walls layer uses a TileSet with physics collision
		const string stdTilesetPath = "res://resources/tilesets/sennen_tiles.tres";
		if (wallsLayer.TileSet == null || wallsLayer.TileSet.GetPhysicsLayersCount() == 0)
		{
			if (ResourceLoader.Exists(stdTilesetPath))
				wallsLayer.TileSet = GD.Load<TileSet>(stdTilesetPath);
			else
				return;
		}

		// Gather all ground layers (including dynamically created ones like MappTiles)
		var groundLayers = GetAllGroundLayers();
		if (groundLayers.Count == 0) return;

		// Compute the union bounding rect of all ground layers
		Rect2I bounds = default;
		bool first = true;
		foreach (var gl in groundLayers)
		{
			var r = gl.GetUsedRect();
			if (!r.HasArea()) continue;
			if (first) { bounds = r; first = false; }
			else bounds = bounds.Merge(r);
		}
		if (!bounds.HasArea()) return;

		Vector2I wallAtlas = new(2, 6);
		int wallSourceId = 0;
		int wallCount = 0;

		// Expand by 1 tile on each side for border walls
		var expanded = new Rect2I(
			bounds.Position - Vector2I.One,
			bounds.Size + new Vector2I(2, 2));

		for (int x = expanded.Position.X; x < expanded.End.X; x++)
		{
			for (int y = expanded.Position.Y; y < expanded.End.Y; y++)
			{
				var pos = new Vector2I(x, y);

				// Skip if ANY ground layer has a tile here
				bool hasGround = false;
				foreach (var gl in groundLayers)
				{
					if (gl.GetCellSourceId(pos) >= 0) { hasGround = true; break; }
				}
				if (hasGround) continue;

				// Skip if wall tile already exists here
				if (wallsLayer.GetCellSourceId(pos) >= 0) continue;

				wallsLayer.SetCell(pos, wallSourceId, wallAtlas);
				wallCount++;
			}
		}

		if (wallCount > 0)
			GD.Print($"[OverworldBase] FillUnpaintedTilesAsWalls: painted {wallCount} wall tiles " +
				$"from {groundLayers.Count} ground layers (physics: {wallsLayer.TileSet?.GetPhysicsLayersCount() ?? 0})");
	}

	/// <summary>
	/// Reads the Ground TileMapLayer's used rect and applies it as camera limits
	/// to the Camera2D inside the player scene.
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

		// Apply to the player's Camera2D so it clamps movement to the map bounds.
		var cam = _player.GetNodeOrNull<Camera2D>("Camera2D");
		if (cam != null)
		{
			cam.LimitLeft   = left;
			cam.LimitTop    = top;
			cam.LimitRight  = right;
			cam.LimitBottom = bottom;
		}

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
