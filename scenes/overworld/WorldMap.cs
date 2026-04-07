using Godot;
using Godot.Collections;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Overworld.Forage;

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

	// Forage cooldown floor — guarantees at least N steps between forage minigames so
	// the player isn't pulled into back-to-back rhythm prompts. Increments every step,
	// resets after a successful forage trigger.
	private const int ForageCooldownSteps = 10;
	private int _stepsSinceLastForage = ForageCooldownSteps;

	// Active minigame instance — non-null while the player is mid-prompt.
	private ForageMinigame? _activeForageMinigame;

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
		SpawnEntranceLabels();
		SpawnParallaxBackground();

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

		// 5. Foraging roll (cooldown floor between minigames)
		_stepsSinceLastForage++;
		if (TryForage()) return;

		// 6. Random encounter roll
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

	// ── Foraging ──────────────────────────────────────────────────────────────

	private bool TryForage()
	{
		if (GameManager.Instance.CurrentState != GameState.Overworld) return false;
		if (DialogicBridge.Instance.IsRunning()) return false;
		if (_activeForageMinigame != null) return false;
		if (_stepsSinceLastForage < ForageCooldownSteps) return false;

		double roll = GD.RandRange(0.0, 100.0);
		if (!ForageLogic.ShouldForage(roll)) return false;

		_stepsSinceLastForage = 0;

		// Settings toggle: when the minigame is disabled, fall back to the legacy
		// instant-grant path (one default-table item, no rhythm prompt).
		bool minigameEnabled = SettingsManager.Instance?.Current.ForageMinigameEnabled ?? true;
		if (!minigameEnabled)
		{
			GrantForageItems(ForageLogic.ForageGrade.Miss);
			return true;
		}

		LaunchForageMinigame();
		return true;
	}

	/// <summary>
	/// Spawn the rhythm minigame as a top-of-screen overlay, freeze player input via
	/// GameState.Dialog, and connect the completion signal.
	/// Note count scales gently with BPM so faster songs feel slightly more challenging.
	/// </summary>
	private void LaunchForageMinigame()
	{
		const string scenePath = "res://scenes/overworld/forage/ForageMinigame.tscn";
		if (!ResourceLoader.Exists(scenePath))
		{
			// Asset missing — fall back to instant grant so the feature degrades gracefully.
			GD.PushWarning($"[WorldMap] ForageMinigame scene missing at {scenePath}; granting fallback item.");
			GrantForageItems(ForageLogic.ForageGrade.Miss);
			return;
		}

		var minigame = GD.Load<PackedScene>(scenePath).Instantiate<ForageMinigame>();
		_activeForageMinigame = minigame;

		// Host on a CanvasLayer so it floats above the world without scaling oddly.
		var layer = new CanvasLayer { Layer = 51 };
		AddChild(layer);
		layer.AddChild(minigame);

		// Center the minigame near the top of the screen.
		var viewportSize = GetViewportRect().Size;
		minigame.Position = new Vector2((viewportSize.X - 320f) * 0.5f, viewportSize.Y * 0.18f);

		minigame.ForageCompleted += OnForageCompleted;
		minigame.ForageCancelled += OnForageCancelled;

		float bpm       = AudioManager.Instance?.GetCurrentBgmBpm() ?? RhythmConstants.DefaultBpm;
		int   noteCount = bpm > 110f ? 5 : 4;

		GameManager.Instance.SetState(GameState.Dialog); // freeze player input
		minigame.Activate(noteCount);
	}

	private void OnForageCancelled()
	{
		// ESC bail — treat as Miss, still grant baseline item so the player isn't punished.
		// OnForageCompleted will fire next from the minigame's Finish() and handle cleanup.
	}

	private void OnForageCompleted(int perfects, int hits, int totalNotes)
	{
		var grade = ForageLogic.GradeFromAccuracy(hits, perfects, totalNotes);
		GrantForageItems(grade);
		TeardownActiveForageMinigame();
	}

	private void TeardownActiveForageMinigame()
	{
		if (_activeForageMinigame == null) return;

		// The minigame was added under a wrapper CanvasLayer in LaunchForageMinigame.
		var layer = _activeForageMinigame.GetParent();
		_activeForageMinigame.ForageCompleted -= OnForageCompleted;
		_activeForageMinigame.ForageCancelled -= OnForageCancelled;
		_activeForageMinigame = null;
		layer?.QueueFree();
	}

	/// <summary>
	/// Grant items for the given grade and play the forage_found dialog.
	/// Selects items independently from the grade-biased table so the player can
	/// receive a mix of common/rare drops on a strong run.
	/// </summary>
	private void GrantForageItems(ForageLogic.ForageGrade grade)
	{
		int    count = ForageLogic.BonusItemCount(grade);
		var    table = ForageLogic.WeightedTableForGrade(grade);
		string firstName = "something";
		bool   anyGranted = false;

		for (int i = 0; i < count; i++)
		{
			double itemRoll = GD.RandRange(0.0, 1.0);
			string path = ForageLogic.SelectForageItem(itemRoll, table);
			if (!ResourceLoader.Exists(path)) continue;

			var item = GD.Load<ItemData>(path);
			if (item == null) continue;

			GameManager.Instance.AddItem(path);
			if (!anyGranted)
			{
				firstName  = item.DisplayName;
				anyGranted = true;
			}
		}

		if (!anyGranted)
		{
			// Nothing was actually granted (broken table?) — restore overworld and bail.
			GameManager.Instance.SetState(GameState.Overworld);
			return;
		}

		GD.Print($"[WorldMap] Foraged ({grade}): {count}× starting with {firstName}");

		GameManager.Instance.SetState(GameState.Dialog);
		var bridge = DialogicBridge.Instance;
		bridge.SetVariable("forage_item_name", firstName);
		bridge.SetVariable("forage_grade",     ForageLogic.GradeArticle(grade));
		bridge.SetVariable("forage_count",     count);
		bridge.ConnectTimelineEnded(
			Callable.From(() => GameManager.Instance.SetState(GameState.Overworld)));
		bridge.StartTimelineWithFlags("res://dialog/timelines/forage_found.dtl");
	}

	// ── Random encounter ──────────────────────────────────────────────────────

	private void TryRollEncounter(Vector2I tile)
	{
		var gm = GameManager.Instance;
		if (gm.DebugNoEncounters) return;

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
		// Check collision layer first — if a collision tile exists, check its data
		var tileData = _collision.GetCellTileData(tile);
		if (tileData != null)
			return !tileData.GetCustomData("impassable").AsBool();

		// No collision tile — check if a ground tile exists at this position.
		// If no ground tile exists, the tile is impassable (empty space).
		var groundLayer = GetNodeOrNull<TileMapLayer>("Ground");
		if (groundLayer != null)
			return groundLayer.GetCellSourceId(tile) >= 0;

		return false; // No ground layer = impassable
	}

	// ── BGM ───────────────────────────────────────────────────────────────────

	private void ApplyDayNightBgm(bool animate)
	{
		string path = GameManager.Instance.IsNight ? NightBgmPath : DayBgmPath;
		if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path)) return;
		AudioManager.Instance.PlayBgm(path, fadeTime: animate ? 2.5f : 0.2f);
	}

	// ── Landmark labels ───────────────────────────────────────────────────────

	private void SpawnEntranceLabels()
	{
		// With canvas_items stretch mode, world-space labels scale with everything.
		// No CanvasLayer needed.
		foreach (Node child in _entrances.GetChildren())
		{
			if (child is not WorldMapEntrance entrance) continue;
			if (string.IsNullOrEmpty(entrance.EntranceName)) continue;

			var label = new Label
			{
				Text                = entrance.EntranceName,
				HorizontalAlignment = HorizontalAlignment.Center,
				Position            = entrance.Position + new Vector2(-40f, -14f),
				CustomMinimumSize   = new Vector2(80f, 0f),
				LabelSettings       = new LabelSettings
				{
					Font         = UiTheme.LoadPixelFont(),
					FontSize     = 8,
					FontColor    = UiTheme.Gold,
					OutlineSize  = 1,
					OutlineColor = Colors.Black,
				},
				TextureFilter = TextureFilterEnum.Nearest,
			};
			AddChild(label);
		}
	}

	// ── Parallax background ───────────────────────────────────────────────────

	private void SpawnParallaxBackground()
	{
		// Render clouds as world-space Node2D children with high ZIndex
		// so they appear above tiles. With canvas_items stretch they scale correctly.
		var cloudContainer = new Node2D { ZIndex = 100 };
		AddChild(cloudContainer);

		var rng = new RandomNumberGenerator();
		rng.Seed = 42;

		for (int i = 0; i < 15; i++)
		{
			float cx = rng.RandfRange(10f, 310f);
			float cy = rng.RandfRange(10f, 220f);
			float w  = rng.RandfRange(30f, 70f);
			float h  = rng.RandfRange(8f, 18f);
			float alpha = rng.RandfRange(0.03f, 0.08f);

			var cloud = new ColorRect
			{
				Color    = new Color(1f, 1f, 1f, alpha),
				Position = new Vector2(cx, cy),
				Size     = new Vector2(w, h),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			cloudContainer.AddChild(cloud);

			// Slow drift animation
			var drift = CreateTween().SetLoops();
			float driftDist = rng.RandfRange(10f, 30f);
			float driftTime = rng.RandfRange(8f, 16f);
			drift.TweenProperty(cloud, "position:x", cx + driftDist, driftTime);
			drift.TweenProperty(cloud, "position:x", cx, driftTime);
		}
	}
}
