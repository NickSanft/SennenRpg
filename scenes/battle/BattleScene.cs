using Godot;
using System.Threading.Tasks;
using System.Threading;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

public enum BattleState { Intro, PlayerTurn, EnemyTurn, RhythmPhase, StrikePhase, SkillPhase, Victory, Defeat }

/// <summary>
/// Root battle scene state machine — rhythm RPG version.
/// Fight uses RhythmStrike (single-beat timing).
/// Enemy turn uses RhythmArena (4-lane note highway).
/// PERFORM opens the Bard skills sub-menu (skills implemented in Phase 5).
/// All dialog is displayed through Dialogic timelines in dialog/timelines/battle_*.dtl.
/// </summary>
public partial class BattleScene : Node2D
{
	private enum SubMenuMode { Perform, Mercy, Items }

	private BattleState _state;
	private EnemyData?  _enemy;
	private int         _enemyCurrentHp;
	private bool        _enemyCanBeSpared;
	private int         _mercyPercent;
	private SubMenuMode _subMenuMode;

	// ── Node references ───────────────────────────────────────────────
	private Node2D          _enemyArea      = null!;
	private ActionMenu      _actionMenu     = null!;
	private SubMenu         _subMenu        = null!;
	private BattleHUD       _battleHud      = null!;
	private RhythmArena     _rhythmArena    = null!;
	private RhythmStrike    _rhythmStrike   = null!;
	private EnemyNameplate  _enemyNameplate = null!;
	private Node2D          _enemyVisual    = null!;

	private CharmMinigame    _charmMinigame  = null!;
	private BardMinigameBase[] _bardSkills   = null!;
	private int              _currentSkillIndex;
	private PerformanceScore _performance    = new();

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
		_actionMenu.MercySelected   += OnMercySelected;

		_subMenu.OptionSelected += OnSubMenuOptionSelected;
		_subMenu.Cancelled      += OnSubMenuCancelled;

		// Wire rhythm nodes
		_rhythmStrike.StrikeResolved += OnStrikeResolved;
		_rhythmArena.PhaseEnded      += OnRhythmPhaseEnded;
		_rhythmArena.PlayerHurt      += OnPlayerHurt;

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

		_charmMinigame.CharmCompleted += OnCharmCompleted;
		foreach (var skill in _bardSkills)
			skill.SkillCompleted += OnSkillCompleted;

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

		_enemyCurrentHp = _enemy?.Stats?.MaxHp ?? 10;

		// Start battle BGM with BPM
		StartBattleBgm(encounter);

		SetupEnemySprite();
		_enemyNameplate.Setup(_enemy?.DisplayName ?? "???");
		_ = RunIntro();
	}

	// ── BGM ───────────────────────────────────────────────────────────

	private const string DefaultBattleBgmPath =
		"res://assets/music/Divora - Ominous Augury - DND 7 - 10 Corruption Can Be Fun.wav";

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

		if (ResourceLoader.Exists(bgmPath))
		{
			AudioManager.Instance.PlayBgm(bgmPath, fadeTime: 0.1f, bpm: bpm);
		}
		else
		{
			RhythmClock.Instance.StartFreeRunning(bpm);
			GD.Print($"[BattleScene] No battle BGM found. RhythmClock free-running at {bpm} BPM.");
		}
	}

	// ── Setup ─────────────────────────────────────────────────────────

	private void SetupEnemySprite()
	{
		if (_enemy?.BattleSprite != null)
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

		_enemyArea.AddChild(_enemyVisual);

		var tween = CreateTween().SetLoops();
		tween.TweenProperty(_enemyVisual, "position:y",  4f, 0.6f)
			 .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(_enemyVisual, "position:y", -4f, 0.6f)
			 .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
	}

	// ── State machine ─────────────────────────────────────────────────

	private void SetState(BattleState newState)
	{
		_state = newState;
		_actionMenu.Visible     = newState == BattleState.PlayerTurn;
		_subMenu.Visible        = false;
		_rhythmStrike.Visible   = false;
		_rhythmArena.Visible    = false;
		_enemyNameplate.Visible = newState is BattleState.PlayerTurn or BattleState.EnemyTurn;

		_charmMinigame.Visible = false;
		foreach (var skill in _bardSkills)
			skill.Visible = false;

		GD.Print($"[BattleScene] State → {newState}");

		if (newState == BattleState.PlayerTurn)
			_actionMenu.FocusFirst();
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
		SetBattleVar("enemy_name", _enemy?.DisplayName ?? "???");
		await RunBattleTimeline("res://dialog/timelines/battle_intro.dtl");
		SetState(BattleState.PlayerTurn);
	}

	// ── Fight — RhythmStrike ──────────────────────────────────────────

	private void OnFightSelected() => _ = DoFightSelected();

	private async Task DoFightSelected()
	{
		GD.Print("[BattleScene] STRIKE selected.");
		_actionMenu.Visible = false;
		SetState(BattleState.StrikePhase);
		await RunBattleTimeline("res://dialog/timelines/battle_strike_prompt.dtl");
		_rhythmStrike.Visible = true;
		_rhythmStrike.Activate();
	}

	private void OnStrikeResolved(int gradeInt) => _ = DoStrikeResolved(gradeInt);

	private async Task DoStrikeResolved(int gradeInt)
	{
		var grade = (HitGrade)gradeInt;
		_rhythmStrike.Visible = false;

		int   atk    = GameManager.Instance.PlayerStats.Attack;
		int   def    = _enemy?.Stats?.Defense ?? 0;
		float mult   = RhythmConstants.GradeMultiplier(grade);
		int   damage = Mathf.Max(1, Mathf.RoundToInt(atk * mult) - def);
		_enemyCurrentHp -= damage;

		bool   isCrit   = grade == HitGrade.Perfect;
		string hitLabel = grade switch
		{
			HitGrade.Perfect => "Perfect hit!",
			HitGrade.Good    => "Hit!",
			_                => "Weak hit."
		};

		GD.Print($"[BattleScene] {hitLabel} grade={grade}, mult={mult:F2}, damage={damage}. Enemy HP: {_enemyCurrentHp}");
		SpawnDamageNumber(damage, isCrit);

		SetBattleVar("hit_label",   hitLabel);
		SetBattleVar("enemy_name",  _enemy?.DisplayName ?? "???");
		SetBattleVar("damage",      damage.ToString());
		await RunBattleTimeline("res://dialog/timelines/battle_hit.dtl");

		if (_enemyCurrentHp <= 0)
			await HandleVictory(killed: true);
		else
			await RunEnemyTurn();
	}

	// ── Perform (Bard Skills) ─────────────────────────────────────────

	private void OnPerformSelected()
	{
		GD.Print("[BattleScene] PERFORM selected");
		_subMenuMode = SubMenuMode.Perform;
		_actionMenu.Visible = false;

		string[] options = _enemy?.BardicActOptions is { Length: > 0 }
			? _enemy.BardicActOptions
			: DefaultBardSkillNames;

		_subMenu.Populate(options);
		_subMenu.Visible = true;
	}

	// ── Item ──────────────────────────────────────────────────────────

	private void OnItemSelected() => _ = DoItemSelected();

	private async Task DoItemSelected()
	{
		var inv = GameManager.Instance.InventoryItemPaths;
		if (inv.Count == 0)
		{
			await RunBattleTimeline("res://dialog/timelines/battle_no_items.dtl");
			await RunEnemyTurn();
			return;
		}

		_subMenuMode = SubMenuMode.Items;
		_actionMenu.Visible = false;

		var labels = new string[inv.Count];
		for (int i = 0; i < inv.Count; i++)
		{
			var item = ResourceLoader.Exists(inv[i]) ? GD.Load<ItemData>(inv[i]) : null;
			labels[i] = item != null ? $"{item.DisplayName} (+{item.HealAmount} HP)" : "???";
		}
		_subMenu.Populate(labels);
		_subMenu.Visible = true;
	}

	// ── Mercy ─────────────────────────────────────────────────────────

	private void OnMercySelected()
	{
		GD.Print("[BattleScene] Mercy selected");
		_subMenuMode = SubMenuMode.Mercy;
		_actionMenu.Visible = false;

		string spareLabel = _enemyCanBeSpared ? "Spare" : $"Spare ({_mercyPercent}%)";
		_subMenu.Populate([spareLabel, "Flee"]);
		_subMenu.Visible = true;
	}

	// ── Sub-menu dispatch ─────────────────────────────────────────────

	private void OnSubMenuOptionSelected(int index) => _ = DoSubMenuOptionSelected(index);

	private async Task DoSubMenuOptionSelected(int index)
	{
		_subMenu.Visible = false;

		if (_subMenuMode == SubMenuMode.Mercy) { await HandleMercyOption(index); return; }
		if (_subMenuMode == SubMenuMode.Items)  { await HandleItemOption(index);  return; }

		// Perform mode — launch the Bard skill minigame
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

	private async Task HandleMercyOption(int index)
	{
		if (index == 0)
		{
			if (_enemyCanBeSpared)
			{
				await HandleVictory(killed: false);
			}
			else
			{
				await RunBattleTimeline("res://dialog/timelines/battle_mercy_ignored.dtl");
				await RunEnemyTurn();
			}
		}
		else
		{
			bool escaped = GD.RandRange(0, 1) == 0;
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
	}

	private void OnActDialogEnded()
	{
		GameManager.Instance.SetState(GameState.Battle);
		_ = RunEnemyTurn();
	}

	private void OnSkillCompleted(int gradeInt) => _ = DoSkillCompleted(gradeInt);

	private async Task DoSkillCompleted(int gradeInt)
	{
		var grade = (HitGrade)gradeInt;

		int baseMercy = (_enemy?.SkillMercyValues is { Length: > 0 } && _currentSkillIndex < _enemy.SkillMercyValues.Length)
			? _enemy.SkillMercyValues[_currentSkillIndex]
			: 20;

		int mercyGain = Mathf.RoundToInt(baseMercy * RhythmConstants.GradeMultiplier(grade));
		_mercyPercent = Mathf.Clamp(_mercyPercent + mercyGain, 0, 100);
		_enemyCanBeSpared = _mercyPercent >= 100 && (_enemy?.CanBeSpared ?? false);
		_enemyNameplate.UpdateMercy(_mercyPercent, _enemyCanBeSpared);

		string result = grade switch
		{
			HitGrade.Perfect => "★ Perfect performance! The enemy is moved.",
			HitGrade.Good    => "♪ Good! The enemy responds warmly.",
			_                => "♩ The performance falls flat...",
		};

		GD.Print($"[BattleScene] Skill grade={grade}, mercy+{mercyGain} → {_mercyPercent}%");

		SetBattleVar("skill_result", result);
		string path = _enemyCanBeSpared
			? "res://dialog/timelines/battle_skill_spare_ready.dtl"
			: "res://dialog/timelines/battle_skill_result.dtl";
		await RunBattleTimeline(path);

		await RunEnemyTurn();
	}

	private void OnCharmCompleted(int successCount, int totalNotes) => _ = DoCharmCompleted(successCount, totalNotes);

	private async Task DoCharmCompleted(int successCount, int totalNotes)
	{
		float ratio = totalNotes > 0 ? (float)successCount / totalNotes : 0f;
		var grade = ratio >= 0.75f ? HitGrade.Perfect : ratio >= 0.5f ? HitGrade.Good : HitGrade.Miss;

		int baseMercy = 30;
		int mercyGain = Mathf.RoundToInt(baseMercy * RhythmConstants.GradeMultiplier(grade));
		_mercyPercent = Mathf.Clamp(_mercyPercent + mercyGain, 0, 100);
		_enemyCanBeSpared = _mercyPercent >= 100 && (_enemy?.CanBeSpared ?? false);
		_enemyNameplate.UpdateMercy(_mercyPercent, _enemyCanBeSpared);

		string result = grade switch
		{
			HitGrade.Perfect => "★ Charmed! The enemy can't resist.",
			HitGrade.Good    => "♪ The enemy seems swayed.",
			_                => "♩ The charm attempt fizzles.",
		};

		GD.Print($"[BattleScene] Charm {successCount}/{totalNotes}, grade={grade}, mercy+{mercyGain} → {_mercyPercent}%");

		SetBattleVar("charm_result",  result);
		SetBattleVar("notes_success", successCount.ToString());
		SetBattleVar("notes_total",   totalNotes.ToString());
		string path = _enemyCanBeSpared
			? "res://dialog/timelines/battle_charm_spare_ready.dtl"
			: "res://dialog/timelines/battle_charm_result.dtl";
		await RunBattleTimeline(path);

		await RunEnemyTurn();
	}

	private async Task HandleItemOption(int index)
	{
		var inv = GameManager.Instance.InventoryItemPaths;
		if (index >= inv.Count)
		{
			await RunBattleTimeline("res://dialog/timelines/battle_item_nothing.dtl");
			await RunEnemyTurn();
			return;
		}

		string path = inv[index];
		var item = ResourceLoader.Exists(path) ? GD.Load<ItemData>(path) : null;
		if (item == null)
		{
			await RunBattleTimeline("res://dialog/timelines/battle_item_gone.dtl");
			await RunEnemyTurn();
			return;
		}

		GameManager.Instance.RemoveItem(path);
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

	// ── Enemy turn ────────────────────────────────────────────────────

	private async Task RunEnemyTurn()
	{
		SetState(BattleState.EnemyTurn);

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

	private void BeginRhythmPhase()
	{
		SetState(BattleState.RhythmPhase);

		PackedScene? patternScene = _enemy?.AttackPatternScene;
		_rhythmArena.StartPhase(patternScene, totalMeasures: 2);

		GD.Print($"[BattleScene] RhythmPhase started. Pattern: {patternScene?.ResourcePath ?? "none"}");
	}

	private void OnRhythmPhaseEnded()
	{
		if (_state != BattleState.RhythmPhase) return;
		GD.Print("[BattleScene] Rhythm phase ended.");
		SetState(BattleState.PlayerTurn);
	}

	private void OnPlayerHurt(int damage)
	{
		GameManager.Instance.HurtPlayer(damage);
		int hp = GameManager.Instance.PlayerStats.CurrentHp;
		GD.Print($"[BattleScene] Player hurt for {damage}. HP: {hp}");

		if (hp <= 0)
		{
			_rhythmArena.Visible = false;
			_ = HandleDefeat();
		}
	}

	// ── Victory / Defeat / Flee ───────────────────────────────────────

	private async Task HandleVictory(bool killed)
	{
		SetState(BattleState.Victory);
		RhythmClock.Instance.Stop();
		AudioManager.Instance.StopBgm(fadeTime: 0.5f);

		if (killed)
		{
			int gold = _enemy?.GoldDrop ?? 0;
			int exp  = _enemy?.ExpDrop  ?? 0;
			GameManager.Instance.RegisterKill();
			GameManager.Instance.AddGold(gold);
			GameManager.Instance.AddExp(exp);

			SetBattleVar("gold_gained",          gold.ToString());
			SetBattleVar("exp_gained",            exp.ToString());
			SetBattleVar("love",                  GameManager.Instance.Love.ToString());
			SetBattleVar("performance_summary",   _performance.GetSummaryText());
			await RunBattleTimeline("res://dialog/timelines/battle_victory.dtl");
		}
		else
		{
			GameManager.Instance.RegisterSpare();
			await RunBattleTimeline("res://dialog/timelines/battle_spared.dtl");
		}

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
		string map = GameManager.Instance.LastMapPath;
		if (string.IsNullOrEmpty(map))
			map = "res://scenes/overworld/TestRoom.tscn";
		await SceneTransition.Instance.GoToAsync(map);
	}
}
