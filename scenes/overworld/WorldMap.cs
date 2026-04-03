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
	private const string DayBgmPath   = "res://assets/music/Drifting in the Astral Paring (Ambient).wav";
	private const string NightBgmPath = "res://assets/music/Drifting in the Astral Paring.wav";

	[Export] public Array<EncounterData> DayEncounters   { get; set; } = new();
	[Export] public Array<EncounterData> NightEncounters { get; set; } = new();

	private TileMapLayer    _collision = null!;
	private Node2D          _entrances = null!;
	private WorldMapPlayer  _player    = null!;
	private DayNightOverlay _dayNight  = null!;

	// Step counter: encounters are gated by a minimum step count between rolls.
	private int _stepsSinceLastEncounter = 0;
	private int _nextEncounterThreshold  = 0; // initialised in _Ready

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
		gm.SetLastMap("res://scenes/overworld/WorldMap.tscn");
		gm.SetState(GameState.Overworld);

		// Spawn PauseMenu so ESC works on the world map
		const string pausePath = "res://scenes/menus/PauseMenu.tscn";
		if (ResourceLoader.Exists(pausePath))
			AddChild(GD.Load<PackedScene>(pausePath).Instantiate());

		ApplyDayNightBgm(animate: false);

		_nextEncounterThreshold = (int)GD.RandRange(8, 14);
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

		// 3. Tick Mellyr Outpost passive rewards
		var tick = TownRewardLogic.TryTick(
			gm.TownStepCounter,
			gm.GetFlag(Flags.NpcRainPurchased),
			gm.PendingRainGold,
			gm.GetFlag(Flags.NpcLilyPurchased),
			gm.PendingLilyRecipes.Count,
			gm.PlayerLevel);
		gm.TownStepCounter = tick.NewCounter;
		gm.PendingRainGold = tick.NewPendingRainGold;
		if (tick.LilyRecipe != null)
			gm.PendingLilyRecipes.Add(tick.LilyRecipe);

		// 4. Day/Night flip
		if (DayNightLogic.ShouldFlip(gm.TilesWalkedOnWorldMap))
		{
			gm.IsNight = DayNightLogic.ApplyFlip(gm.IsNight);
			_ = _dayNight.AnimateTransition(gm.IsNight);
			ApplyDayNightBgm(animate: true);
		}

		// 5. Random encounter roll
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
		var gm = GameManager.Instance;

		// Repel: block encounters and consume one step of protection.
		if (gm.RepelStepsRemaining > 0)
		{
			gm.RepelStepsRemaining--;
			return;
		}

		var enc = gm.IsNight ? NightEncounters : DayEncounters;
		if (enc.Count == 0) return;

		// Step counter: enforce a minimum gap between encounters.
		_stepsSinceLastEncounter++;
		if (_stepsSinceLastEncounter < _nextEncounterThreshold) return;

		// Reset counter now (whether or not an encounter fires).
		_stepsSinceLastEncounter = 0;
		_nextEncounterThreshold  = (int)GD.RandRange(8, 14);

		// Encounter-rate multiplier from settings (Low = 30%, Off = 0%).
		float mult = SettingsLogic.EncounterRateMultiplier(
			SettingsManager.Instance?.Current.EncounterRateMode ?? EncounterRateMode.Normal);
		if (mult <= 0f || GD.Randf() >= mult) return;

		var chosen = enc[(int)GD.RandRange(0, enc.Count - 1)];
		gm.WorldMapReturnTile = tile;
		_ = SceneTransition.Instance.ToBattleAsync(chosen);
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
		if (tileData == null) return true;    // no collision tile painted = passable
		// AsBool() returns false when the custom data key doesn't exist (Nil variant)
		return !tileData.GetCustomData("impassable").AsBool();
	}

	// ── BGM ───────────────────────────────────────────────────────────────────

	private void ApplyDayNightBgm(bool animate)
	{
		string path = GameManager.Instance.IsNight ? NightBgmPath : DayBgmPath;
		if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path)) return;
		AudioManager.Instance.PlayBgm(path, fadeTime: animate ? 2.5f : 0.2f);
	}
}
