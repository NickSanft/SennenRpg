using Godot;
using Godot.Collections;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Tile-based world map. Manages a grid-locked player, entrance detection,
/// random encounters, and the day/night cycle.
/// Scene structure: WorldMap (Node2D) → Ground, Collision (TileMapLayer),
/// Entrances (Node2D with WorldMapEntrance children).
/// </summary>
public partial class WorldMap : Node2D
{
	private const string PlayerScene  = "res://scenes/overworld/WorldMapPlayer.tscn";
	private const string BattleScene  = "res://scenes/battle/BattleScene.tscn";
	private const string DayBgmPath   = "";  // set to a world BGM path when available
	private const string NightBgmPath = "";

	[Export] public Array<EncounterData> DayEncounters   { get; set; } = new();
	[Export] public Array<EncounterData> NightEncounters { get; set; } = new();

	private TileMapLayer    _collision = null!;
	private Node2D          _entrances = null!;
	private WorldMapPlayer  _player    = null!;
	private DayNightOverlay _dayNight  = null!;

	public override void _Ready()
	{
		_collision = GetNode<TileMapLayer>("Collision");
		_entrances = GetNode<Node2D>("Entrances");

		// Spawn the player
		_player = GD.Load<PackedScene>(PlayerScene).Instantiate<WorldMapPlayer>();
		_player.StepTaken += OnStepTaken;
		AddChild(_player);

		// Restore player position: prefer return tile (coming back from an indoor map/battle)
		var gm = GameManager.Instance;
		Vector2I spawnTile;
		if (gm.WorldMapReturnTile != Vector2I.Zero)
		{
			spawnTile = gm.WorldMapReturnTile;
			gm.WorldMapReturnTile = Vector2I.Zero;
		}
		else
		{
			spawnTile = gm.WorldMapSpawnTile;
		}
		_player.Position = WorldMapPlayer.TileToWorld(spawnTile);

		// Day/night overlay
		_dayNight = new DayNightOverlay();
		AddChild(_dayNight);
		_dayNight.ApplyImmediate(gm.IsNight);

		// Register this as the current map so battle returns here
		gm.LastMapPath = "res://scenes/overworld/WorldMap.tscn";
		gm.SetState(GameState.Overworld);

		ApplyDayNightBgm(animate: false);
	}

	// ── Step handler ─────────────────────────────────────────────────────────

	private void OnStepTaken(Vector2I newTile)
	{
		var gm = GameManager.Instance;

		// 1. Check entrances before any counter logic
		foreach (Node child in _entrances.GetChildren())
		{
			if (child is WorldMapEntrance entrance && entrance.MatchesTile(newTile))
			{
				_ = EnterLocation(entrance);
				return;
			}
		}

		// 2. Increment persistent tile counter
		gm.TilesWalkedOnWorldMap++;

		// 3. Day/Night flip
		if (DayNightLogic.ShouldFlip(gm.TilesWalkedOnWorldMap))
		{
			gm.IsNight = DayNightLogic.ApplyFlip(gm.IsNight);
			_ = _dayNight.AnimateTransition(gm.IsNight);
			ApplyDayNightBgm(animate: true);
		}

		// 4. Random encounter roll
		TryRollEncounter(newTile);
	}

	// ── Entrance / scene transition ───────────────────────────────────────────

	private async Task EnterLocation(WorldMapEntrance entrance)
	{
		if (string.IsNullOrEmpty(entrance.TargetScenePath)) return;

		GameManager.Instance.WorldMapReturnTile = entrance.ReturnTile;
		GameManager.Instance.SetState(GameState.Battle); // block input during transition
		await SceneTransition.Instance.GoToAsync(entrance.TargetScenePath);
	}

	// ── Random encounter ──────────────────────────────────────────────────────

	private void TryRollEncounter(Vector2I tile)
	{
		var gm  = GameManager.Instance;
		var enc = gm.IsNight ? NightEncounters : DayEncounters;
		if (enc.Count == 0) return;

		float rate = EncounterLogic.EncounterRate(gm.EffectiveStats.Luck, gm.IsNight);
		if (GD.Randf() >= rate) return;

		var chosen = enc[(int)GD.RandRange(0, enc.Count - 1)];
		BattleRegistry.Instance.SetPendingEncounter(chosen);

		// Store current tile so we return here after the battle
		gm.WorldMapReturnTile = tile;
		_ = SceneTransition.Instance.GoToAsync(BattleScene);
	}

	// ── Passability ───────────────────────────────────────────────────────────

	/// <summary>
	/// Called by WorldMapPlayer before committing a move.
	/// Returns false for tiles with the "impassable" custom data flag set,
	/// or for tiles outside the map bounds (null tile data).
	/// Returns true (passable) when no Collision TileMapLayer is assigned.
	/// </summary>
	public bool IsTilePassable(Vector2I tile)
	{
		var tileData = _collision.GetCellTileData(tile);
		if (tileData == null) return false;   // out of bounds / empty = blocked
		return !(bool)(tileData.GetCustomData("impassable") ?? (Variant)false);
	}

	// ── BGM ───────────────────────────────────────────────────────────────────

	private void ApplyDayNightBgm(bool animate)
	{
		string path = GameManager.Instance.IsNight ? NightBgmPath : DayBgmPath;
		if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path)) return;
		AudioManager.Instance.PlayBgm(path, fadeTime: animate ? 2.5f : 0.2f);
	}
}
