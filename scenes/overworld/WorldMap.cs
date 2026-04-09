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

	// Weather overlay spawned on this map — removed on exit.
	private SennenRpg.Scenes.Hud.WeatherOverlay? _weatherOverlay;

	// Phase 4 — overworld follower chain. The WorldMap is a 16×16 sprite map so it
	// always spawns followers when the party has more than one member.
	private FollowerTrail? _followerTrail;
	private System.Collections.Generic.List<SennenRpg.Scenes.Player.PartyFollower> _followers = new();
	// Tracks the leader's position from BEFORE the latest step. Pushing this into the
	// trail (instead of the leader's current position) means stepsBack=1 returns the
	// tile directly behind the leader, not the leader's own tile.
	private Vector2 _lastLeaderPos;
	private bool _subscribedToPartyOrder;

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

		// Spawn the per-member overworld HUD on the world map too.
		const string hudPath = "res://scenes/hud/GameHud.tscn";
		if (ResourceLoader.Exists(hudPath))
			AddChild(GD.Load<PackedScene>(hudPath).Instantiate());

		// Spawn PauseMenu so ESC works on the world map
		const string pausePath = "res://scenes/menus/PauseMenu.tscn";
		if (ResourceLoader.Exists(pausePath))
			AddChild(GD.Load<PackedScene>(pausePath).Instantiate());

		ApplyDayNightBgm(animate: false);
		SpawnEntranceLabels();
		SpawnParallaxBackground();
		SetupWeather();

		_nextEncounterThreshold = (int)GD.RandRange(8, 14);

		// Phase 4: spawn party followers (16×16 sprite map → always-on).
		// First make sure the leader's sprite matches whoever is currently leading.
		ApplyLeaderSpriteToPlayer();
		SpawnFollowers();

		// Refresh leader sprite + follower chain whenever the party order changes
		// (e.g. via the Party Menu's set-leader / swap actions).
		var gmForSig = GameManager.Instance;
		if (gmForSig != null)
		{
			gmForSig.PartyOrderChanged += OnPartyOrderChanged;
			_subscribedToPartyOrder = true;
		}
	}

	private void OnPartyOrderChanged()
	{
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
		_player.SetSpriteSheet(path);
	}

	private void RespawnFollowers()
	{
		// Tear down existing followers and rebuild from the new party order.
		foreach (var f in _followers)
		{
			if (IsInstanceValid(f)) f.QueueFree();
		}
		_followers.Clear();
		_followerTrail = null;
		SpawnFollowers();
	}

	// ── Party followers (Phase 4) ────────────────────────────────────

	private void SpawnFollowers()
	{
		var party = GameManager.Instance.Party;
		if (party.IsEmpty) return;

		var followerMembers = new System.Collections.Generic.List<SennenRpg.Core.Data.PartyMember>();
		for (int i = 0; i < party.Members.Count; i++)
		{
			if (i == party.LeaderIndex) continue;
			followerMembers.Add(party.Members[i]);
		}
		if (followerMembers.Count == 0) return;

		_followerTrail = new FollowerTrail(capacity: System.Math.Max(8, followerMembers.Count + 2));
		_lastLeaderPos = _player.Position;

		Vector2 spawn = _player.Position;

		// Make sure the leader visually outranks every follower so the player is never
		// covered up when they overlap (spawn frame, intersections at corners, etc.).
		_player.ZIndex = 10;

		for (int i = 0; i < followerMembers.Count; i++)
		{
			var member   = followerMembers[i];
			var follower = new SennenRpg.Scenes.Player.PartyFollower();
			follower.Configure(member.OverworldSpritePath, _followerTrail, stepsBack: i + 1, spawnPosition: spawn);
			follower.ZIndex = 5; // Below the leader, above the ground tiles.
			AddChild(follower);
			_followers.Add(follower);
		}

		GD.Print($"[WorldMap] Spawned {_followers.Count} party follower(s).");
	}

	// ── Step handler ─────────────────────────────────────────────────────────

	private void OnStepTaken(Vector2I newTile)
	{
		var gm = GameManager.Instance;

		// Phase 4: push the leader's PREVIOUS tile so a follower at stepsBack=1 lands one
		// tile behind the leader (not on the leader's own tile). _lastLeaderPos is updated
		// AFTER the push so the next step records this tile as "previous".
		if (_followerTrail != null)
		{
			_followerTrail.Push(_lastLeaderPos);
			_lastLeaderPos = WorldMapPlayer.TileToWorld(newTile);
		}

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
			gm.PlayerLevel,
			gm.GetFlag(Flags.NpcBhataPurchased),
			gm.PendingBhataAles);
		gm.TownStepCounter  = tick.NewCounter;
		gm.PendingRainGold  = tick.NewPendingRainGold;
		gm.PendingBhataAles = tick.NewPendingBhataAles;
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

		// 6. Weather tick — may change weather and swap BGM via OnWeatherChanged
		WeatherManager.Instance?.TickStep();

		// 7. Storm-only lightning loot roll
		MaybeSpawnLightningBolt(newTile);

		// 8. Random encounter roll
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
			GrantForageItems(ForageLogic.ForageGrade.Miss, grantStreakReward: false);
			return true;
		}

		LaunchForageMinigame();
		return true;
	}

	/// <summary>
	/// Spawn the rhythm minigame as a top-of-screen overlay, freeze player input via
	/// GameState.Dialog, and connect the completion signal.
	/// Note count scales gently with BPM so faster songs feel slightly more challenging.
	/// Forager's Eye Ranger cross-class bonus adds one extra note slot.
	/// </summary>
	private void LaunchForageMinigame()
	{
		const string scenePath = "res://scenes/overworld/forage/ForageMinigame.tscn";
		if (!ResourceLoader.Exists(scenePath))
		{
			// Asset missing — fall back to instant grant so the feature degrades gracefully.
			GD.PushWarning($"[WorldMap] ForageMinigame scene missing at {scenePath}; granting fallback item.");
			GrantForageItems(ForageLogic.ForageGrade.Miss, grantStreakReward: false);
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

		// Forager's Eye (Ranger Lv5 cross-class) — one extra note slot.
		if (HasForagersEye())
			noteCount += 1;

		GameManager.Instance.SetState(GameState.Dialog); // freeze player input
		minigame.Activate(noteCount);
	}

	/// <summary>
	/// True when the player has earned the Forager's Eye cross-class bonus
	/// (Ranger Lv5). Grants +1 forage item count and +1 note slot in the minigame.
	/// </summary>
	private static bool HasForagersEye()
	{
		var levels = new System.Collections.Generic.Dictionary<PlayerClass, int>();
		foreach (var (cls, entry) in GameManager.Instance.ClassEntries)
			levels[cls] = entry.Level;
		return MultiClassLogic.HasTag(levels, CrossClassBonus.ForagersEye);
	}

	private void OnForageCancelled()
	{
		// ESC bail — treat as Miss, still grant baseline item so the player isn't punished.
		// OnForageCompleted will fire next from the minigame's Finish() and handle cleanup.
	}

	private void OnForageCompleted(int perfects, int hits, int totalNotes)
	{
		var grade = ForageLogic.GradeFromAccuracy(hits, perfects, totalNotes);
		var gm    = GameManager.Instance;

		// Check the streak threshold BEFORE we update the counter for this run.
		// The plan: "At streak >= 5 the NEXT forage guarantees an Astral Flower."
		// So a player who hit 5 perfects in a row gets the rare drop on the
		// 6th forage, regardless of how that 6th run grades.
		bool grantStreakReward = ForageLogic.ShouldGrantStreakReward(gm.ForageStreak);

		GrantForageItems(grade, grantStreakReward);

		// Update the streak counter — Perfect increments, anything else resets.
		// If we just consumed a streak reward, the next streak starts fresh
		// regardless of this run's grade.
		gm.ForageStreak = grantStreakReward
			? 0
			: ForageLogic.NextStreak(gm.ForageStreak, grade);

		TeardownActiveForageMinigame();

		// Mini-juice: a Perfect grade gets a gold screen flash and a soft fanfare.
		if (grade == ForageLogic.ForageGrade.Perfect)
		{
			AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/victory_fanfare.wav");
			FlashGoldOverlay();
		}
	}

	/// <summary>
	/// Brief gold-tinted full-screen flash on a Perfect forage. Self-cleaning ColorRect
	/// added to its own CanvasLayer so it doesn't fight the existing scene tree.
	/// </summary>
	private void FlashGoldOverlay()
	{
		var layer = new CanvasLayer { Layer = 99 };
		AddChild(layer);

		var flash = new ColorRect
		{
			Color       = new Color(1f, 0.85f, 0.2f, 0.55f),
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		layer.AddChild(flash);

		var tween = CreateTween();
		tween.TweenProperty(flash, "color:a", 0f, 0.45f).SetTrans(Tween.TransitionType.Sine);
		tween.TweenCallback(Callable.From(() => layer.QueueFree()));
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
	///
	/// Applies, in order:
	///   - Forager's Eye Ranger cross-class bonus → +1 item count
	///   - Streak system → first item swapped for Astral Flower when streak is at threshold
	///   - Day/Night biasing → night doubles slime/hairball weights
	///   - Codex recording → unlocks first-discovery chime per item
	/// </summary>
	private void GrantForageItems(ForageLogic.ForageGrade grade, bool grantStreakReward)
	{
		var gm = GameManager.Instance;

		int count = ForageLogic.BonusItemCount(grade);
		if (HasForagersEye()) count += 1;

		var phase = ForageLogic.ToDayPhase(gm.IsNight);
		var table = ForageLogic.WeightedTableForGrade(grade, phase);

		string firstName  = "something";
		bool   anyGranted = false;
		bool   anyNewDiscovery = false;

		for (int i = 0; i < count; i++)
		{
			string path;
			if (grantStreakReward && i == 0)
			{
				// Streak reward: replace the first roll with a guaranteed Astral Flower.
				path = ForageLogic.AstralFlowerPath;
			}
			else
			{
				double itemRoll = GD.RandRange(0.0, 1.0);
				path = ForageLogic.SelectForageItem(itemRoll, table);
			}

			if (!ResourceLoader.Exists(path)) continue;
			var item = GD.Load<ItemData>(path);
			if (item == null) continue;

			gm.AddItem(path);

			// Codex: deterministic UTC timestamp injection.
			if (gm.ForageCodex.Record(path, grade, System.DateTime.UtcNow))
				anyNewDiscovery = true;

			if (!anyGranted)
			{
				firstName  = item.DisplayName;
				anyGranted = true;
			}
		}

		if (!anyGranted)
		{
			gm.SetState(GameState.Overworld);
			return;
		}

		if (anyNewDiscovery)
			AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/area_chime.wav");

		GD.Print($"[WorldMap] Foraged ({grade}, streak {gm.ForageStreak}): {count}× starting with {firstName}");

		gm.SetState(GameState.Dialog);
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

		// Weather-biased selection: encounters whose PreferredWeather list contains
		// the current weather get a 2× weight. Everything else stays at 1×.
		// The threshold/frequency logic below runs unchanged — we only influence
		// which encounter wins when one is picked.
		int currentWeatherInt = (int)(WeatherManager.Instance?.Current ?? SennenRpg.Core.Data.WeatherType.Sunny);

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

		var chosen = PickWeatherBiasedEncounter(enc, currentWeatherInt);
		if (chosen == null) return;

		gm.WorldMapReturnTile = tile;
		_ = SceneTransition.Instance.ToBattleAsync(chosen);
	}

	/// <summary>
	/// Weighted random encounter selection honoring each entry's
	/// <see cref="EncounterData.PreferredWeather"/>. Pure math lives in
	/// <see cref="EncounterLogic.WeatherWeightMultiplier"/>.
	/// </summary>
	private static EncounterData? PickWeatherBiasedEncounter(Array<EncounterData> enc, int currentWeather)
	{
		if (enc.Count == 0) return null;

		float totalWeight = 0f;
		var weights = new float[enc.Count];
		for (int i = 0; i < enc.Count; i++)
		{
			var e = enc[i];
			int[] preferred = e?.PreferredWeather?.ToArray() ?? [];
			weights[i] = EncounterLogic.WeatherWeightMultiplier(currentWeather, preferred);
			totalWeight += weights[i];
		}

		float roll = (float)GD.RandRange(0.0, (double)totalWeight);
		float cumulative = 0f;
		for (int i = 0; i < enc.Count; i++)
		{
			cumulative += weights[i];
			if (roll < cumulative) return enc[i];
		}
		return enc[^1];
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

	// ── Weather ───────────────────────────────────────────────────────────────

	/// <summary>
	/// Initialise WeatherManager for this map: cache the map's sunny BGM path,
	/// unlock the weather system, subscribe to WeatherChanged, spawn the overlay,
	/// and immediately apply the current weather's BGM if it differs from Sunny.
	/// </summary>
	private void SetupWeather()
	{
		var wm = WeatherManager.Instance;
		if (wm == null) return;

		// WorldMap uses day/night-specific tracks as its "sunny" baseline.
		// ResolveSunnyBgmPath() recomputes this on-demand from the current IsNight.
		wm.SunnyBgmPath = ResolveSunnyBgmPath();
		wm.Locked       = false;

		wm.WeatherChanged += OnWeatherChanged;

		// Spawn overlay
		_weatherOverlay = new SennenRpg.Scenes.Hud.WeatherOverlay();
		AddChild(_weatherOverlay);

		// Apply current weather's BGM immediately (e.g. if the player loaded a save
		// mid-storm, the weather track should resume without waiting for a roll).
		ApplyWeatherBgm(wm.Current, animate: false);
	}

	private void OnWeatherChanged(int weatherInt)
	{
		var weather = (SennenRpg.Core.Data.WeatherType)weatherInt;
		ApplyWeatherBgm(weather, animate: true);

		// Update WeatherManager's cached sunny path so the next roll back to Sunny
		// restores the correct day/night track.
		WeatherManager.Instance!.SunnyBgmPath = ResolveSunnyBgmPath();
	}

	/// <summary>Returns the world-map BGM that should play under Sunny weather right now.</summary>
	private static string ResolveSunnyBgmPath()
		=> GameManager.Instance.IsNight ? NightBgmPath : DayBgmPath;

	private void ApplyWeatherBgm(SennenRpg.Core.Data.WeatherType weather, bool animate)
	{
		string path = SennenRpg.Core.Data.WeatherLogic.BgmPathFor(weather, ResolveSunnyBgmPath());
		if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path)) return;
		AudioManager.Instance.PlayBgm(path, fadeTime: animate ? 2.5f : 0.5f);
	}

	/// <summary>
	/// Rare lightning loot event during Stormy weather. ~0.5% per step — grants
	/// the Charged Bark key item and plays a gold flash + fanfare SFX.
	/// </summary>
	private void MaybeSpawnLightningBolt(Vector2I tile)
	{
		var wm = WeatherManager.Instance;
		if (wm == null || wm.Current != SennenRpg.Core.Data.WeatherType.Stormy) return;

		double roll = GD.RandRange(0.0, 100.0);
		if (!SennenRpg.Core.Data.WeatherLogic.ShouldStrikeLightning(roll)) return;

		const string barkPath = "res://resources/items/key_charged_bark.tres";
		if (!ResourceLoader.Exists(barkPath))
		{
			GD.PushWarning($"[WorldMap] Lightning strike fired but {barkPath} is missing.");
			return;
		}

		GameManager.Instance.AddItem(barkPath);
		AudioManager.Instance?.PlaySfx("res://assets/audio/sfx/victory_fanfare.wav");
		FlashGoldOverlay();
		GD.Print($"[WorldMap] Lightning strike! Granted Charged Bark at tile {tile}.");
	}

	public override void _ExitTree()
	{
		var wm = WeatherManager.Instance;
		if (wm != null)
		{
			wm.WeatherChanged -= OnWeatherChanged;
		}
		if (_weatherOverlay != null && IsInstanceValid(_weatherOverlay))
			_weatherOverlay.QueueFree();
		_weatherOverlay = null;

		if (_subscribedToPartyOrder)
		{
			var gm = GameManager.Instance;
			if (gm != null) gm.PartyOrderChanged -= OnPartyOrderChanged;
			_subscribedToPartyOrder = false;
		}
	}

	// ── Debug input ───────────────────────────────────────────────────────────

	/// <summary>
	/// Debug-build only —
	///   K = peek the current weather state in the console
	///   J = force the weather to Aurora for testing
	/// </summary>
	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true } keyEvent) return;
		if (!OS.IsDebugBuild()) return;

		if (keyEvent.Keycode == Key.K)
		{
			var wm = WeatherManager.Instance;
			if (wm != null)
			{
				GD.Print($"[Debug] Weather: {SennenRpg.Core.Data.WeatherLogic.DisplayName(wm.Current)} " +
					$"(step {wm.StepCounter % wm.RollInterval}/{wm.RollInterval})");
			}
			GetViewport().SetInputAsHandled();
		}
		else if (keyEvent.Keycode == Key.J)
		{
			var wm = WeatherManager.Instance;
			if (wm != null)
			{
				GD.Print($"[Debug] Forcing weather → Aurora (was {SennenRpg.Core.Data.WeatherLogic.DisplayName(wm.Current)})");
				wm.ForceSet(SennenRpg.Core.Data.WeatherType.Aurora);
			}
			GetViewport().SetInputAsHandled();
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
