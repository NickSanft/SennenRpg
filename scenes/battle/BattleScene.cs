using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using SennenRpg.Core.Extensions;
using SennenRpg.Scenes.Menus;

namespace SennenRpg.Scenes.Battle;

public enum BattleState { Intro, PlayerTurn, EnemyTurn, RhythmPhase, StrikePhase, SkillPhase, Victory, Defeat }

/// <summary>
/// Root battle scene state machine — rhythm RPG version.
/// Fight is routed by class: Bard→RhythmStrike, Fighter→FightBar, Ranger→RangerAim, Mage→MageRuneInput.
/// Enemy turn uses RhythmArena (4-lane note highway).
/// PERFORM opens the Bard skills sub-menu (skills implemented in Phase 5).
/// All dialog is displayed through Dialogic timelines in dialog/timelines/battle_*.dtl.
/// </summary>
public partial class BattleScene : Node2D
{
	private enum SubMenuMode { Perform, Items, Spells }

	private BattleState _state;
	private EnemyData?  _enemy;
	private int         _enemyCurrentHp;
	private SubMenuMode _subMenuMode;
	private readonly System.Collections.Generic.List<int> _itemIndexMap = new();
	private bool        _playerGoesFirst;
	private float           _difficultyMultiplier = 1f;
	private AdaptationResult _adaptation = RhythmMemoryLogic.ComputeAdaptation(null);
	private bool            _adaptedDialogShown;

	// ── Node references ───────────────────────────────────────────────
	private Node2D          _enemyArea      = null!;
	private ActionMenu      _actionMenu     = null!;
	private SubMenu         _subMenu        = null!;
	private BattleHUD       _battleHud      = null!;
	private RhythmArena     _rhythmArena    = null!;
	private RhythmStrike    _rhythmStrike   = null!;
	private EnemyNameplate  _enemyNameplate = null!;
	private Node2D          _enemyVisual    = null!;
	private ShaderMaterial? _hitFlashMat;

	private LevelUpScreen      _levelUpScreen      = null!;
	private CharmMinigame      _charmMinigame      = null!;
	private BardMinigameBase[] _bardSkills         = null!;
	private ShadowBoltMinigame _shadowBoltMinigame = null!;
	private FightBar           _fightBar           = null!;
	private RangerAim          _rangerAim          = null!;
	private MageRuneInput      _mageRuneInput      = null!;
	private int                _currentSkillIndex;
	private SpellData?         _currentSpell;
	private List<SpellData>    _knownSpells        = new();
	private PerformanceScore   _performance        = new();

	// Extracted helpers
	private readonly BattleStatusEffects _statuses = new();

	private static readonly string[] DefaultBardSkillNames =
		{ "Bardic Inspiration", "Lullaby", "War Cry", "Serenade", "Dissonance" };

	private PackedScene? _damageNumberScene;

	public override void _Ready()
	{
		_enemyArea      = GetNode<Node2D>("EnemyArea");
		_actionMenu     = GetNode<ActionMenu>("ActionMenu");
		_subMenu        = GetNode<SubMenu>("SubMenu");
		_battleHud      = GetNode<BattleHUD>("BattleHUD");
		_rhythmArena    = GetNode<RhythmArena>("RhythmArena");
		_rhythmStrike   = GetNode<RhythmStrike>("RhythmStrike");
		_enemyNameplate = GetNode<EnemyNameplate>("EnemyNameplate");

		// Hide the old native dialog box — all dialog now goes through Dialogic
		GetNodeOrNull<Control>("DialogBox")?.Hide();

		const string dmgPath = "res://scenes/battle/ui/DamageNumber.tscn";
		if (ResourceLoader.Exists(dmgPath))
			_damageNumberScene = GD.Load<PackedScene>(dmgPath);

		// Wire action menu
		_actionMenu.FightSelected   += OnFightSelected;
		_actionMenu.PerformSelected += OnPerformSelected;
		_actionMenu.ItemSelected    += OnItemSelected;
		_actionMenu.FleeSelected    += OnFleeSelected;

		// Wire Dialogic status signals (e.g. "status:poison:3" applies Poison for 3 turns)
		DialogicBridge.Instance.DialogicSignalReceived += OnDialogicSignalReceived;

		_subMenu.OptionSelected += OnSubMenuOptionSelected;
		_subMenu.Cancelled      += OnSubMenuCancelled;

		// Wire rhythm nodes
		_rhythmStrike.StrikeResolved += OnStrikeResolved;
		_rhythmArena.PhaseEnded      += OnRhythmPhaseEnded;
		_rhythmArena.PlayerHurt      += OnPlayerHurt;

		// Wire class-specific Fight minigames
		_fightBar      = GetNode<FightBar>("FightBar");
		_rangerAim     = GetNode<RangerAim>("RangerAim");
		_mageRuneInput = GetNode<MageRuneInput>("MageRuneInput");
		_fightBar.Confirmed      += OnFighterConfirmed;
		_rangerAim.Confirmed     += OnRangerConfirmed;
		_mageRuneInput.Completed += OnMageCompleted;

		// Instantiate Charm and Bard skills programmatically
		_charmMinigame = InstantiateFullRect<CharmMinigame>("res://scenes/battle/rhythm/CharmMinigame.tscn");
		_bardSkills = new BardMinigameBase[]
		{
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/BardicInspirationMinigame.tscn"),
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/LullabyMinigame.tscn"),
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/WarCryMinigame.tscn"),
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/SerenadeMinigame.tscn"),
			InstantiateFullRect<BardMinigameBase>("res://scenes/battle/rhythm/skills/DissonanceMinigame.tscn"),
		};

		_levelUpScreen = GD.Load<PackedScene>("res://scenes/menus/LevelUpScreen.tscn")
							.Instantiate<LevelUpScreen>();
		AddChild(_levelUpScreen);

		_shadowBoltMinigame = InstantiateFullRect<ShadowBoltMinigame>(
			"res://scenes/battle/rhythm/skills/ShadowBoltMinigame.tscn");
		_shadowBoltMinigame.SkillCompleted += OnSpellMinigameCompleted;

		_charmMinigame.CharmCompleted += OnCharmCompleted;
		foreach (var skill in _bardSkills)
			skill.SkillCompleted += OnSkillCompleted;

		// Load known spells
		foreach (string path in GameManager.Instance.KnownSpellPaths)
		{
			if (ResourceLoader.Exists(path))
				_knownSpells.Add(GD.Load<SpellData>(path));
		}

		// Performance tracking
		_rhythmArena.NoteHit    += grade => _performance.Record((HitGrade)grade);
		_rhythmArena.PlayerHurt += _ => _performance.Record(HitGrade.Miss);
		_rhythmStrike.StrikeResolved += grade => _performance.Record((HitGrade)grade);

		// Load encounter
		var encounter = BattleRegistry.Instance.GetPendingEncounter();
		if (encounter != null && encounter.Enemies.Count > 0)
			_enemy = encounter.Enemies[0];
		else
			GD.PushWarning("[BattleScene] No pending encounter — using placeholder enemy.");

		_difficultyMultiplier = SettingsLogic.EnemyDifficultyMultiplier(
			SettingsManager.Instance?.Current.BattleDifficulty ?? BattleDifficulty.Normal);
		_enemyCurrentHp = Math.Max(1, (int)((_enemy?.Stats?.MaxHp ?? 10) * _difficultyMultiplier));

		// Rhythm Memory: look up how this enemy has adapted to the player
		string enemyId = _enemy?.EnemyId ?? "";
		GameManager.Instance.RhythmMemory.TryGetValue(enemyId, out var rhythmHistory);
		_adaptation = RhythmMemoryLogic.ComputeAdaptation(rhythmHistory);
		GD.Print($"[BattleScene] Rhythm Memory lookup: enemy={enemyId}, " +
			$"history={rhythmHistory?.TotalEncounters ?? 0} encounters " +
			$"(P:{rhythmHistory?.TotalPerfects ?? 0} G:{rhythmHistory?.TotalGoods ?? 0} M:{rhythmHistory?.TotalMisses ?? 0}), " +
			$"tier={_adaptation.Tier}, density=×{_adaptation.ObstacleDensityMult:F2}, measures=+{_adaptation.ExtraMeasures}");

		_playerGoesFirst = BattleFormulas.PlayerGoesFirst(
			GameManager.Instance.EffectiveStats.Speed,
			_enemy?.Stats?.Speed ?? 0);

		GD.Print($"[BattleScene] Turn order: {(_playerGoesFirst ? "Player" : "Enemy")} goes first " +
				 $"(player SPD={GameManager.Instance.EffectiveStats.Speed}, enemy SPD={_enemy?.Stats?.Speed ?? 0})");

		// Start battle BGM with BPM
		StartBattleBgm(encounter);

		SetupBattleBackground();
		SetupEnemySprite();
		_enemyNameplate.Setup(_enemy?.DisplayName ?? "???");
		_ = RunIntro();
	}

	// ── BGM ───────────────────────────────────────────────────────────

	private const string DefaultBattleBgmPath =
		"res://assets/music/Corruption Can Be Fun.wav";

	private void StartBattleBgm(EncounterData? encounter)
	{
		string bgmPath = encounter?.BattleBgmPath ?? "";
		if (string.IsNullOrEmpty(bgmPath))
			bgmPath = _enemy?.BattleBgmPath ?? "";
		if (string.IsNullOrEmpty(bgmPath))
			bgmPath = DefaultBattleBgmPath;

		float bpm = (encounter?.BattleBpm ?? 0f) > 0f ? encounter!.BattleBpm
				  : (_enemy?.BattleBpm ?? 0f) > 0f    ? _enemy!.BattleBpm
				  : RhythmConstants.DefaultBpm;

		float beatOffset = (_enemy?.BattleBeatOffsetSec ?? 0f);

		if (ResourceLoader.Exists(bgmPath))
		{
			AudioManager.Instance.PlayBgm(bgmPath, fadeTime: 0.1f, bpm: bpm,
			beatOffsetSec: beatOffset, forceRestart: true);
		}
		else
		{
			RhythmClock.Instance.StartFreeRunning(bpm);
			GD.Print($"[BattleScene] No battle BGM found. RhythmClock free-running at {bpm} BPM.");
		}
	}

	// ── Setup ─────────────────────────────────────────────────────────

	private void SetupBattleBackground()
	{
		var baseColor = BattleRegistry.Instance.PendingBackgroundColor;
		// Create gradient: lighter at top, darker at bottom
		var topColor    = baseColor.Lerp(Colors.White, 0.3f) with { A = 0.6f };
		var bottomColor = baseColor.Lerp(Colors.Black, 0.4f) with { A = 0.8f };

		// Use two overlapping ColorRects for a simple two-tone gradient
		var bgTop = new ColorRect
		{
			Color         = topColor,
			AnchorRight   = 1f,
			AnchorBottom  = 0.5f,
			MouseFilter   = Control.MouseFilterEnum.Ignore,
			ZIndex        = -10,
		};
		var bgBottom = new ColorRect
		{
			Color         = bottomColor,
			AnchorTop     = 0.5f,
			AnchorRight   = 1f,
			AnchorBottom  = 1f,
			MouseFilter   = Control.MouseFilterEnum.Ignore,
			ZIndex        = -10,
		};
		AddChild(bgTop);
		AddChild(bgBottom);
		MoveChild(bgTop, 0);
		MoveChild(bgBottom, 1);
	}

	private void SetupEnemySprite()
	{
		if (_enemy?.BattleSprite != null && _enemy.SpriteFrameCount > 0)
		{
			var tex = _enemy.BattleSprite;
			int size = _enemy.SpriteFrameSize;
			var frames = new SpriteFrames();
			frames.AddAnimation("idle");
			frames.SetAnimationLoop("idle", true);
			frames.SetAnimationSpeed("idle", _enemy.SpriteAnimFps);

			for (int f = 0; f < _enemy.SpriteFrameCount; f++)
			{
				var atlas = new AtlasTexture
				{
					Atlas  = tex,
					Region = new Rect2(f * size, 0, size, size),
				};
				frames.AddFrame("idle", atlas);
			}

			var animated = new AnimatedSprite2D { SpriteFrames = frames };
			animated.Play("idle");
			_enemyVisual = animated;
		}
		else if (_enemy?.BattleSprite != null)
		{
			var sprite = new Sprite2D { Texture = _enemy.BattleSprite };
			_enemyVisual = sprite;
		}
		else
		{
			var poly = new Polygon2D
			{
				Polygon = [
					new Vector2(-20, -28), new Vector2(20, -28),
					new Vector2(20,  28),  new Vector2(-20, 28)
				],
				Color = new Color(0.55f, 0.3f, 0.85f, 1f)
			};
			_enemyVisual = poly;
		}

		// Apply hit flash shader
		const string flashShaderPath = "res://assets/shaders/hit_flash.gdshader";
		if (ResourceLoader.Exists(flashShaderPath) && _enemyVisual is CanvasItem ci)
		{
			_hitFlashMat = new ShaderMaterial { Shader = GD.Load<Shader>(flashShaderPath) };
			ci.Material = _hitFlashMat;
		}

		if (_enemyVisual is Node2D visual)
			visual.Scale = new Vector2(4f, 4f);

		_enemyArea.AddChild(_enemyVisual);
	}

	public override void _ExitTree()
	{
		if (DialogicBridge.Instance != null)
			DialogicBridge.Instance.DialogicSignalReceived -= OnDialogicSignalReceived;
	}

	// ── State machine ─────────────────────────────────────────────────

	private void SetState(BattleState newState)
	{
		_state = newState;
		_subMenu.Visible        = false;
		_rhythmStrike.Visible   = false;
		_enemyNameplate.Visible = newState is BattleState.PlayerTurn or BattleState.EnemyTurn;

		_charmMinigame.Visible      = false;
		_shadowBoltMinigame.Visible = false;
		foreach (var skill in _bardSkills)
			skill.Visible = false;

		// Hide class minigames (visible set individually when activated)
		_fightBar.Visible      = false;
		_rangerAim.Visible     = false;
		_mageRuneInput.Visible = false;

		GD.Print($"[BattleScene] State → {newState}");

		if (newState == BattleState.PlayerTurn)
		{
			int chance = BattleFormulas.FleeChance(
				GameManager.Instance.EffectiveStats.Speed,
				_enemy?.Stats?.Speed ?? 0);
			_actionMenu.SetFleeLabel($"Flee ({chance}%)");
			_actionMenu.SlideIn();
			_actionMenu.FocusFirst();
			_battleHud.SetHints(BattleHints.PlayerTurn);
		}
		else
		{
			_actionMenu.Visible = false;
		}

		if (newState == BattleState.RhythmPhase)
		{
			_battleHud.SetHints(BattleHints.RhythmPhase);
		}
		else
		{
			_rhythmArena.Visible = false;
		}
	}

	private T InstantiateFullRect<T>(string path) where T : Control
	{
		var node = GD.Load<PackedScene>(path).Instantiate<T>();
		node.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		node.Visible = false;
		AddChild(node);
		return node;
	}

	private void SpawnDamageNumber(int damage, bool isCrit)
	{
		if (_damageNumberScene == null) return;
		var num = _damageNumberScene.Instantiate<DamageNumber>();
		num.Position = _enemyArea.Position + new Vector2((float)GD.RandRange(-16.0, 16.0), -30f);
		AddChild(num);
		num.Play(damage, isCrit);
	}

	/// <summary>Brief slow-motion on critical hits for dramatic impact.</summary>
	private async void PlayCritSlowMotion()
	{
		Engine.TimeScale = 0.3;
		await ToSignal(GetTree().CreateTimer(0.15f * 0.3f), SceneTreeTimer.SignalName.Timeout);
		Engine.TimeScale = 1.0;
	}

	/// <summary>Briefly flashes the enemy sprite white via the hit_flash shader.</summary>
	private void FlashEnemy()
	{
		if (_hitFlashMat == null) return;
		_hitFlashMat.SetShaderParameter("flash_amount", 1.0f);
		var t = CreateTween();
		t.TweenMethod(Callable.From<float>(v => _hitFlashMat.SetShaderParameter("flash_amount", v)),
			1.0f, 0.0f, 0.08f);
	}

	// ── Dialogic helpers ──────────────────────────────────────────────

	/// <summary>Set a Dialogic variable for use in the next battle timeline.</summary>
	private void SetBattleVar(string name, Variant value)
		=> DialogicBridge.Instance.SetVariable(name, value);

	/// <summary>
	/// Start a battle timeline and await its completion.
	/// Falls back silently (with a warning) if the timeline file doesn't exist.
	/// </summary>
	private async Task RunBattleTimeline(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			GD.PushWarning($"[BattleScene] Timeline not found: {path}");
			return;
		}

		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		DialogicBridge.Instance.ConnectTimelineEnded(Callable.From(() => tcs.TrySetResult(true)));
		DialogicBridge.Instance.StartTimeline(path);

		// Safety timeout — if timeline_ended never fires, unblock after 10 s
		_ = Task.Delay(10_000).ContinueWith(_ => tcs.TrySetResult(true));

		await tcs.Task;
	}

	// ── Intro ─────────────────────────────────────────────────────────

	private async Task RunIntro()
	{
		SetState(BattleState.Intro);

		// Enemy intro zoom: start small, bounce to full size
		if (_enemyVisual != null)
		{
			_enemyVisual.Scale = new Vector2(2f, 2f);
			var zoomTween = CreateTween();
			zoomTween.TweenProperty(_enemyVisual, "scale", new Vector2(4f, 4f), 0.3f)
				.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
			await ToSignal(zoomTween, Tween.SignalName.Finished);
		}

		SetBattleVar("enemy_name", _enemy?.DisplayName ?? "???");
		await RunBattleTimeline("res://dialog/timelines/battle_intro.dtl");

		if (_playerGoesFirst)
			await BeginPlayerTurn();
		else
			await RunEnemyTurn();
	}

	// ── Fight — routed by player class ───────────────────────────────

	private void OnFightSelected() => _ = DoFightSelected();

	private async Task DoFightSelected()
	{
		_actionMenu.SlideOut();
		SetState(BattleState.StrikePhase);
		await RunBattleTimeline("res://dialog/timelines/battle_strike_prompt.dtl");

		var playerClass = GameManager.Instance.PlayerStats.Class;
		GD.Print($"[BattleScene] FIGHT selected. Class={playerClass}");

		switch (playerClass)
		{
			case PlayerClass.Fighter:
				_battleHud.SetHints(BattleHints.FighterTiming);
				_fightBar.Visible = true;
				_fightBar.Activate();
				break;

			case PlayerClass.Ranger:
				_battleHud.SetHints(BattleHints.RangerAim);
				_rangerAim.Visible = true;
				_rangerAim.Activate();
				break;

			case PlayerClass.Mage:
				_battleHud.SetHints(BattleHints.MageRunes);
				_mageRuneInput.Visible = true;
				_mageRuneInput.Activate();
				break;

			default: // Bard + fallback
				_rhythmStrike.Visible = true;
				_rhythmStrike.Activate();
				break;
		}
	}

	// ── Fighter — timing bar result ───────────────────────────────────

	private void OnFighterConfirmed(float accuracy)
	{
		_fightBar.Visible = false;
		_ = DoStrikeResolved((int)BattleAttackResolver.ResolveFighterGrade(accuracy));
	}

	// ── Ranger — aim reticle result ───────────────────────────────────

	private void OnRangerConfirmed(float accuracy, bool isCrit)
	{
		_rangerAim.Visible = false;
		if (isCrit)
		{
			_ = DoRangerCrit();
			return;
		}
		_ = DoStrikeResolved((int)BattleAttackResolver.ResolveRangerGrade(accuracy));
	}

	private async Task DoRangerCrit()
	{
		int damage = BattleAttackResolver.ResolveRangerCrit(GameManager.Instance.EffectiveStats.Attack);
		_enemyCurrentHp -= damage;
		CameraShake.ShakeNode(this, intensity: 5f, duration: 0.18f);
		FlashEnemy();
		SpawnDamageNumber(damage, isCrit: true);
		SetBattleVar("hit_label",  "Bull's-eye!");
		SetBattleVar("enemy_name", _enemy?.DisplayName ?? "???");
		SetBattleVar("damage",     damage.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");
		if (_enemyCurrentHp <= 0)
			await HandleVictory();
		else
			await RunEnemyTurn();
	}

	// ── Mage — rune sequence result ───────────────────────────────────

	private void OnMageCompleted(int correctCount)
	{
		_mageRuneInput.Visible = false;
		_ = DoStrikeResolved((int)BattleAttackResolver.ResolveMageGrade(correctCount));
	}

	private void OnStrikeResolved(int gradeInt) => _ = DoStrikeResolved(gradeInt);

	private async Task DoStrikeResolved(int gradeInt)
	{
		var grade = (HitGrade)gradeInt;
		_rhythmStrike.Visible = false;

		var (damage, isCrit, hitLabel) = BattleAttackResolver.ResolveStrike(
			grade,
			GameManager.Instance.EffectiveStats.Attack,
			_enemy?.Stats?.Defense ?? 0,
			GameManager.Instance.EffectiveStats.Luck);
		_enemyCurrentHp -= damage;
		CameraShake.ShakeNode(this, intensity: isCrit ? 5f : 2f, duration: isCrit ? 0.18f : 0.1f);
		FlashEnemy();
		if (isCrit) PlayCritSlowMotion();

		GD.Print($"[BattleScene] {hitLabel} grade={grade}, damage={damage}. Enemy HP: {_enemyCurrentHp}");
		SpawnDamageNumber(damage, isCrit);

		SetBattleVar("hit_label",   hitLabel);
		SetBattleVar("enemy_name",  _enemy?.DisplayName ?? "???");
		SetBattleVar("damage",      damage.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");

		if (_enemyCurrentHp <= 0)
			await HandleVictory();
		else
			await RunEnemyTurn();
	}

	// ── Perform (Bard Skills) ─────────────────────────────────────────

	private void OnPerformSelected()
	{
		GD.Print("[BattleScene] PERFORM selected");
		_subMenuMode = SubMenuMode.Perform;
		_actionMenu.SlideOut();

		string[] bardOptions = _enemy?.BardicActOptions is { Length: > 0 }
			? _enemy.BardicActOptions
			: DefaultBardSkillNames;

		// Append "Spells ▶" if the player knows any spells
		string[] options = _knownSpells.Count > 0
			? bardOptions.Append("Spells ▶").ToArray()
			: bardOptions;

		_subMenu.Populate(options);
		_subMenu.Visible = true;
	}

	// ── Item ──────────────────────────────────────────────────────────

	private void OnItemSelected() => _ = DoItemSelected();

	private async Task DoItemSelected()
	{
		var inv = GameManager.Instance.InventoryItemPaths;

		// Build item list — only show Consumable and Repel items in battle
		_itemIndexMap.Clear();
		var labels = new System.Collections.Generic.List<string>();
		for (int i = 0; i < inv.Count; i++)
		{
			string path = inv[i];
			if (!ResourceLoader.Exists(path)) continue;

			ItemData? item = null;
			try { item = GD.Load<ItemData>(path); } catch { /* ignore load failures */ }
			if (item == null) continue;

			// Only show Consumable and Repel items in battle
			if (item.Type != ItemType.Consumable && item.Type != ItemType.Repel) continue;
			if (item.HealAmount <= 0 && item.RepelSteps <= 0) continue;

			string label = item.RepelSteps > 0
				? $"{item.DisplayName} (Repel {item.RepelSteps} steps)"
				: $"{item.DisplayName} (+{item.HealAmount} HP)";

			labels.Add(label);
			_itemIndexMap.Add(i);
		}

		GD.Print($"[BattleScene] Item menu: {labels.Count} usable items from {inv.Count} total");

		if (labels.Count == 0)
		{
			await RunBattleTimeline("res://dialog/timelines/battle_no_items.dtl");
			await RunEnemyTurn();
			return;
		}

		_subMenuMode = SubMenuMode.Items;
		_actionMenu.SlideOut();
		GD.Print($"[BattleScene] Populating SubMenu with {labels.Count} items");
		_subMenu.Populate(labels.ToArray());
		_subMenu.Visible = true;
		GD.Print($"[BattleScene] SubMenu visible = {_subMenu.Visible}");
	}

	// ── Flee ──────────────────────────────────────────────────────────

	private void OnFleeSelected() => _ = DoFleeSelected();

	private async Task DoFleeSelected()
	{
		GD.Print("[BattleScene] Flee selected");
		_actionMenu.SlideOut();
		bool escaped = BattleFormulas.AttemptFlee(
			GameManager.Instance.EffectiveStats.Speed,
			_enemy?.Stats?.Speed ?? 0,
			GD.Randf());
		if (escaped)
		{
			await HandleFlee();
		}
		else
		{
			await RunBattleTimeline("res://dialog/timelines/battle_flee_blocked.dtl");
			await RunEnemyTurn();
		}
	}

	// ── Sub-menu dispatch ─────────────────────────────────────────────

	private void OnSubMenuOptionSelected(int index) => _ = DoSubMenuOptionSelected(index);

	private async Task DoSubMenuOptionSelected(int index)
	{
		// ── Spells sub-menu ───────────────────────────────────────────────
		if (_subMenuMode == SubMenuMode.Spells)
		{
			_subMenu.Visible = false;
			await HandleSpellOption(index);
			return;
		}

		// ── Perform sub-menu: check if "Spells ▶" was chosen ─────────────
		if (_subMenuMode == SubMenuMode.Perform && _knownSpells.Count > 0)
		{
			string[] bardOptions = _enemy?.BardicActOptions is { Length: > 0 }
				? _enemy.BardicActOptions
				: DefaultBardSkillNames;

			if (index == bardOptions.Length) // "Spells ▶" is appended at this index
			{
				_subMenuMode = SubMenuMode.Spells;
				string[] spellLabels = _knownSpells
					.Select(s => $"{s.DisplayName}  ({s.MpCost} MP)")
					.ToArray();
				_subMenu.Populate(spellLabels); // stays visible, repopulated
				return;
			}
		}

		// ── Items ─────────────────────────────────────────────────────────
		_subMenu.Visible = false;

		if (_subMenuMode == SubMenuMode.Items) { await HandleItemOption(index); return; }

		// ── Perform — bard skill ──────────────────────────────────────────
		GD.Print($"[BattleScene] Skill option {index} selected");
		_currentSkillIndex = index;

		string[] options = _enemy?.BardicActOptions is { Length: > 0 }
			? _enemy.BardicActOptions
			: DefaultBardSkillNames;

		if (index < options.Length && options[index] == "Charm")
		{
			SetState(BattleState.SkillPhase);
			_charmMinigame.Activate();
			return;
		}

		int skillIndex = index < _bardSkills.Length ? index : 0;
		_currentSkillIndex = skillIndex;
		SetState(BattleState.SkillPhase);
		_bardSkills[skillIndex].Activate();
	}

	private void OnSkillCompleted(int gradeInt) => _ = DoSkillCompleted(gradeInt);

	private async Task DoSkillCompleted(int gradeInt)
	{
		var grade = (HitGrade)gradeInt;

		string result = grade switch
		{
			HitGrade.Perfect => "★ Perfect performance! The enemy is moved.",
			HitGrade.Good    => "♪ Good! The enemy responds warmly.",
			_                => "♩ The performance falls flat...",
		};

		GD.Print($"[BattleScene] Skill grade={grade}");

		SetBattleVar("skill_result", result);
		await RunBattleTimeline("res://dialog/timelines/battle_skill_result.dtl");

		await RunEnemyTurn();
	}

	private void OnCharmCompleted(int successCount, int totalNotes) => _ = DoCharmCompleted(successCount, totalNotes);

	private async Task DoCharmCompleted(int successCount, int totalNotes)
	{
		float ratio = totalNotes > 0 ? (float)successCount / totalNotes : 0f;
		var grade = ratio >= 0.75f ? HitGrade.Perfect : ratio >= 0.5f ? HitGrade.Good : HitGrade.Miss;

		string result = grade switch
		{
			HitGrade.Perfect => "★ Charmed! The enemy can't resist.",
			HitGrade.Good    => "♪ The enemy seems swayed.",
			_                => "♩ The charm attempt fizzles.",
		};

		GD.Print($"[BattleScene] Charm {successCount}/{totalNotes}, grade={grade}");

		SetBattleVar("charm_result",  result);
		SetBattleVar("notes_success", successCount.ToString());
		SetBattleVar("notes_total",   totalNotes.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_charm_result.dtl");

		await RunEnemyTurn();
	}

	// ── Spell handling ────────────────────────────────────────────────────────

	private async Task HandleSpellOption(int index)
	{
		if (index >= _knownSpells.Count)
		{
			SetState(BattleState.PlayerTurn);
			return;
		}

		_currentSpell = _knownSpells[index];
		GD.Print($"[BattleScene] Spell selected: {_currentSpell.DisplayName}");

		// Check MP before starting the minigame
		if (!GameManager.Instance.UseMp(_currentSpell.MpCost))
		{
			SetBattleVar("spell_name", _currentSpell.DisplayName);
			SetBattleVar("mp_cost",    _currentSpell.MpCost.ToString());
			await RunBattleTimeline("res://dialog/timelines/spell_no_mp.dtl");
			SetState(BattleState.PlayerTurn);
			return;
		}

		SetState(BattleState.SkillPhase);
		_shadowBoltMinigame.Activate();
	}

	private void OnSpellMinigameCompleted(int gradeInt) => _ = DoSpellCompleted(gradeInt);

	private async Task DoSpellCompleted(int gradeInt)
	{
		var grade = (HitGrade)gradeInt;
		var spell = _currentSpell!;

		if (grade == HitGrade.Miss)
		{
			SetBattleVar("spell_name", spell.DisplayName);
			await RunBattleTimeline("res://dialog/timelines/spell_shadow_bolt_miss.dtl");
		}
		else
		{
			var (damage, isCrit) = BattleAttackResolver.ResolveSpell(
				grade, spell.BasePower,
				GameManager.Instance.EffectiveStats.Magic,
				_enemy?.Stats?.Resistance ?? 0);
			_enemyCurrentHp -= damage;
			CameraShake.ShakeNode(this, intensity: isCrit ? 5f : 2f, duration: isCrit ? 0.18f : 0.1f);
			FlashEnemy();
			SpawnDamageNumber(damage, isCrit);

			string result = isCrit
				? $"★ Perfect! Darkness surges! {damage} magic damage!"
				: $"♪ The bolt connects! {damage} magic damage.";

			SetBattleVar("spell_name",   spell.DisplayName);
			SetBattleVar("spell_result", result);
			SetBattleVar("damage",       damage.ToString());
			await RunBattleTimeline("res://dialog/timelines/spell_shadow_bolt_cast.dtl");

			if (_enemyCurrentHp <= 0) { await HandleVictory(); return; }
		}

		await RunEnemyTurn();
	}

	private async Task HandleItemOption(int index)
	{
		var inv = GameManager.Instance.InventoryItemPaths;
		// Map filtered sub-menu index back to actual inventory index
		int realIndex = index < _itemIndexMap.Count ? _itemIndexMap[index] : -1;
		if (realIndex < 0 || realIndex >= inv.Count)
		{
			await RunBattleTimeline("res://dialog/timelines/battle_item_nothing.dtl");
			await RunEnemyTurn();
			return;
		}

		string path = inv[realIndex];
		var item = ResourceLoader.Exists(path) ? GD.Load<ItemData>(path) : null;
		if (item == null)
		{
			await RunBattleTimeline("res://dialog/timelines/battle_item_gone.dtl");
			await RunEnemyTurn();
			return;
		}

		GameManager.Instance.RemoveItem(path);

		// Repel item: grants world-map encounter immunity for several steps
		if (item.RepelSteps > 0)
		{
			GameManager.Instance.RepelStepsRemaining += item.RepelSteps;
			SetBattleVar("item_name",   item.DisplayName);
			SetBattleVar("heal_amount", item.RepelSteps.ToString());
			await RunBattleTimeline("res://dialog/timelines/battle_item_repel.dtl");
			await RunEnemyTurn();
			return;
		}

		GameManager.Instance.HealPlayer(item.HealAmount);

		SetBattleVar("item_name",   item.DisplayName);
		SetBattleVar("heal_amount", item.HealAmount.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_item_used.dtl");
		await RunEnemyTurn();
	}

	private void OnSubMenuCancelled()
	{
		_subMenu.Visible = false;
		SetState(BattleState.PlayerTurn);
	}

	// ── BeginPlayerTurn — ticks statuses before showing the action menu ─

	private async Task BeginPlayerTurn()
	{
		// Tick all statuses (both sides) at the top of each round
		_statuses.TickAll();
		UpdateStatusHud();

		// Apply player Poison
		if (_statuses.PlayerHasStatus(StatusEffect.Poison))
		{
			int dmg = _statuses.PlayerPoisonDamage(GameManager.Instance.PlayerStats.MaxHp);
			GameManager.Instance.HurtPlayer(dmg);
			SetBattleVar("damage", dmg.ToString());
			await RunBattleTimeline("res://dialog/timelines/battle_poison_player.dtl");
			if (GameManager.Instance.PlayerStats.CurrentHp <= 0)
			{
				await HandleDefeat();
				return;
			}
		}

		// Apply enemy Poison
		if (_statuses.EnemyHasStatus(StatusEffect.Poison))
		{
			int dmg = _statuses.EnemyPoisonDamage(_enemy?.Stats?.MaxHp ?? 10);
			_enemyCurrentHp = Math.Max(0, _enemyCurrentHp - dmg);
			SetBattleVar("damage", dmg.ToString());
			await RunBattleTimeline("res://dialog/timelines/battle_poison_enemy.dtl");
			if (_enemyCurrentHp <= 0)
			{
				await HandleVictory();
				return;
			}
		}

		// Player Stun: skip this turn
		if (_statuses.PlayerHasStatus(StatusEffect.Stun))
		{
			await RunBattleTimeline("res://dialog/timelines/battle_stun_player.dtl");
			await RunEnemyTurn();
			return;
		}

		SetState(BattleState.PlayerTurn);
	}

	// ── Enemy turn ────────────────────────────────────────────────────

	private async Task RunEnemyTurn()
	{
		// Enemy Stun: skip the rhythm phase this turn
		if (_statuses.EnemyHasStatus(StatusEffect.Stun))
		{
			await RunBattleTimeline("res://dialog/timelines/battle_stun_enemy.dtl");
			await BeginPlayerTurn();
			return;
		}

		SetState(BattleState.EnemyTurn);

		// Rhythm Memory: adapted enemies acknowledge the player on first enemy turn
		if (!_adaptedDialogShown && _adaptation.Tier != AdaptationTier.None)
		{
			_adaptedDialogShown = true;
			string adaptMsg = _adaptation.Tier switch
			{
				AdaptationTier.Rival    => $"{_enemy?.DisplayName ?? "???"} locks eyes with you. It won't hold back!",
				AdaptationTier.Hardened => $"{_enemy?.DisplayName ?? "???"} remembers your rhythm!",
				AdaptationTier.Wary     => $"{_enemy?.DisplayName ?? "???"} seems more cautious...",
				AdaptationTier.Cocky    => $"{_enemy?.DisplayName ?? "???"} yawns lazily at you.",
				_                       => "",
			};
			if (!string.IsNullOrEmpty(adaptMsg))
			{
				var adaptColor = _adaptation.Tier switch
				{
					AdaptationTier.Rival    => new Color(1f, 0.2f, 0.2f),
					AdaptationTier.Hardened => new Color(1f, 0.5f, 0.1f),
					AdaptationTier.Wary     => new Color(1f, 0.9f, 0.3f),
					AdaptationTier.Cocky    => new Color(0.5f, 0.8f, 1f),
					_                       => Colors.White,
				};
				await ShowPhaseCard(adaptMsg, adaptColor);
			}
		}

		string battlePath = _enemy?.BattleTimelinePath ?? "";
		if (!string.IsNullOrEmpty(battlePath) && ResourceLoader.Exists(battlePath))
		{
			GameManager.Instance.SetState(GameState.Dialog);
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			DialogicBridge.Instance.ConnectTimelineEnded(Callable.From(() => tcs.TrySetResult(true)));
			DialogicBridge.Instance.StartTimelineWithFlags(battlePath);
			await tcs.Task;
			GameManager.Instance.SetState(GameState.Battle);
		}
		else
		{
			string line = _enemy?.BattleDialogLines is { Length: > 0 }
				? _enemy.BattleDialogLines[(int)GD.RandRange(0, _enemy.BattleDialogLines.Length - 1)]
				: $"* {_enemy?.DisplayName ?? "???"} prepares to attack...";

			SetBattleVar("enemy_dialog", line);
			await RunBattleTimeline("res://dialog/timelines/battle_enemy_dialog.dtl");
		}

		BeginRhythmPhase();
	}

	private void BeginRhythmPhase() => _ = DoBeginRhythmPhase();

	private async Task DoBeginRhythmPhase()
	{
		SetState(BattleState.RhythmPhase);

		if (_adaptation.Tier >= AdaptationTier.Hardened)
			await ShowPhaseCard("⚠  DODGE!  ⚠  (INTENSIFIED)", new Color(1f, 0.15f, 0.15f));
		else if (_adaptation.Tier == AdaptationTier.Cocky)
			await ShowPhaseCard("⚠  DODGE!  ⚠  (WEAKENED)", new Color(0.5f, 0.8f, 1f));
		else
			await ShowPhaseCard("⚠  DODGE!  ⚠", new Color(1f, 0.3f, 0.3f));

		PackedScene? patternScene = _enemy?.AttackPatternScene;
		int totalMeasures = Math.Max(1, 2 + _adaptation.ExtraMeasures);
		_rhythmArena.ObstacleDensityMult = _adaptation.ObstacleDensityMult;
		_rhythmArena.SlideIn();
		_rhythmArena.StartPhase(patternScene, totalMeasures: totalMeasures);

		GD.Print($"[BattleScene] RhythmPhase started. Pattern: {patternScene?.ResourcePath ?? "none"}, measures: {totalMeasures}, density: {_adaptation.ObstacleDensityMult:F2}");
	}

	private async Task ShowPhaseCard(string text, Color color)
	{
		var label = new Label
		{
			Text                = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment   = VerticalAlignment.Center,
			AnchorLeft          = 0f, AnchorTop    = 0f,
			AnchorRight         = 1f, AnchorBottom = 1f,
			Modulate            = color with { A = 0f },
		};
		label.AddThemeFontSizeOverride("font_size", 32);
		AddChild(label);

		var tween = CreateTween();
		tween.TweenProperty(label, "modulate:a", 1f, 0.2f);
		tween.TweenInterval(0.5f);
		tween.TweenProperty(label, "modulate:a", 0f, 0.3f);
		await ToSignal(tween, Tween.SignalName.Finished);
		label.QueueFree();
	}

	private void OnRhythmPhaseEnded()
	{
		if (_state != BattleState.RhythmPhase) return;
		GD.Print($"[BattleScene] Rhythm phase ended. Max combo: {_rhythmArena.MaxStreak}");
		_battleHud.ShowPerformanceSummary(_performance);
		_ = BeginPlayerTurn();
	}

	private void OnPlayerHurt(int damage)
	{
		int scaledDamage = Math.Max(1, (int)(damage * _difficultyMultiplier));
		GameManager.Instance.HurtPlayer(scaledDamage);
		CameraShake.ShakeNode(this, intensity: 3f, duration: 0.12f);
		int hp = GameManager.Instance.PlayerStats.CurrentHp;
		GD.Print($"[BattleScene] Player hurt for {scaledDamage} (raw {damage}, diff ×{_difficultyMultiplier:F2}). HP: {hp}");

		if (hp <= 0)
		{
			_rhythmArena.Visible = false;
			_ = HandleDefeat();
		}
	}

	// ── Victory / Defeat / Flee ───────────────────────────────────────

	private async Task HandleVictory()
	{
		SetState(BattleState.Victory);
		RhythmClock.Instance.Stop();
		AudioManager.Instance.StopBgm(fadeTime: 0.5f);

		// Victory: enemy shrinks and fades out
		if (_enemyVisual != null)
		{
			var shrinkTween = CreateTween().SetParallel();
			shrinkTween.TweenProperty(_enemyVisual, "scale", Vector2.Zero, 0.5f)
				.SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Back);
			shrinkTween.TweenProperty(_enemyVisual, "modulate:a", 0f, 0.5f);
		}

		// Victory fanfare SFX
		const string fanfarePath = "res://assets/audio/sfx/victory_fanfare.wav";
		AudioManager.Instance?.PlaySfx(fanfarePath);

		// Record kill and rhythm performance for quest/adaptation tracking
		if (!string.IsNullOrEmpty(_enemy?.EnemyId))
		{
			GameManager.Instance.RecordKill(_enemy.EnemyId);
			GameManager.Instance.RecordRhythmPerformance(_enemy.EnemyId, _performance);
		}

		// Apply Rhythm Memory bonus rewards
		int baseGold = _enemy?.GoldDrop ?? 0;
		int baseExp  = _enemy?.ExpDrop  ?? 0;
		int gold = (int)(baseGold * (1f + _adaptation.BonusGoldPercent));
		int exp  = (int)(baseExp  * (1f + _adaptation.BonusExpPercent));
		GameManager.Instance.AddGold(gold);
		GameManager.Instance.AddExp(exp);

		if (_adaptation.BonusGoldPercent > 0f || _adaptation.BonusExpPercent > 0f)
			GD.Print($"[BattleScene] Rhythm Memory bonus: gold {baseGold}→{gold} (+{_adaptation.BonusGoldPercent:P0}), exp {baseExp}→{exp} (+{_adaptation.BonusExpPercent:P0})");

		// Bonus loot roll
		if (_adaptation.BonusLootChance > 0f && GD.Randf() < _adaptation.BonusLootChance)
		{
			string lootPath = _enemy?.BonusLootItemPath ?? "";
			if (!string.IsNullOrEmpty(lootPath) && ResourceLoader.Exists(lootPath))
			{
				GameManager.Instance.AddItem(lootPath);
				GD.Print($"[BattleScene] Rhythm Memory bonus loot: {lootPath}");
			}
		}

		// Show level-up screen for each level gained before the victory dialog
		if (GameManager.Instance.PendingLevelUps.Count > 0)
		{
			var pending = new List<LevelUpResult>(GameManager.Instance.PendingLevelUps);
			GameManager.Instance.PendingLevelUps.Clear();
			await _levelUpScreen.ShowAll(pending);
		}

		SetBattleVar("gold_gained",        gold.ToString());
		SetBattleVar("exp_gained",          exp.ToString());
		SetBattleVar("performance_summary", _performance.GetSummaryText());
		await RunBattleTimeline("res://dialog/timelines/battle_victory.dtl");

		await ReturnToOverworld();
	}

	private async Task HandleFlee()
	{
		SetState(BattleState.Victory);
		RhythmClock.Instance.Stop();
		AudioManager.Instance.StopBgm(fadeTime: 0.5f);
		await RunBattleTimeline("res://dialog/timelines/battle_fled.dtl");
		await ReturnToOverworld();
	}

	private async Task HandleDefeat()
	{
		SetState(BattleState.Defeat);
		RhythmClock.Instance.Stop();
		AudioManager.Instance.StopBgm(fadeTime: 1.0f);
		await RunBattleTimeline("res://dialog/timelines/battle_game_over.dtl");
		await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
		await SceneTransition.Instance.GoToAsync("res://scenes/menus/GameOver.tscn");
	}

	private async Task ReturnToOverworld()
	{
		// Force-close any lingering Dialogic dialog (e.g., if safety timeout fired)
		if (DialogicBridge.Instance.IsRunning())
			DialogicBridge.Instance.EndTimeline();

		string map = GameManager.Instance.LastMapPath;
		if (string.IsNullOrEmpty(map))
			map = "res://scenes/overworld/MAPP.tscn";
		await SceneTransition.Instance.GoToAsync(map);
	}

	// ── Status effects ────────────────────────────────────────────────

	private void OnDialogicSignalReceived(Variant argument)
	{
		if (_statuses.TryHandleDialogicSignal(argument.AsString()))
			UpdateStatusHud();
	}

	private void UpdateStatusHud()
	{
		_battleHud.UpdateStatuses(_statuses.PlayerStatuses);
		_enemyNameplate.UpdateStatuses(_statuses.EnemyStatuses);
	}
}
