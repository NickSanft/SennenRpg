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
	private Label           _dialogLabel    = null!;
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
		_dialogLabel    = GetNode<Label>("DialogBox/DialogLabel");
		_actionMenu     = GetNode<ActionMenu>("ActionMenu");
		_subMenu        = GetNode<SubMenu>("SubMenu");
		_battleHud      = GetNode<BattleHUD>("BattleHUD");
		_rhythmArena    = GetNode<RhythmArena>("RhythmArena");
		_rhythmStrike   = GetNode<RhythmStrike>("RhythmStrike");
		_enemyNameplate = GetNode<EnemyNameplate>("EnemyNameplate");

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
		// Resolve BGM path: encounter > enemy > project default
		string bgmPath = encounter?.BattleBgmPath ?? "";
		if (string.IsNullOrEmpty(bgmPath))
			bgmPath = _enemy?.BattleBgmPath ?? "";
		if (string.IsNullOrEmpty(bgmPath))
			bgmPath = DefaultBattleBgmPath;

		// Resolve BPM: encounter > enemy > default (180)
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

	private void ShowDialogText(string text)
	{
		_dialogLabel.Text = text;
		GD.Print($"[BattleScene] Dialog: {text}");
	}

	// ── Intro ─────────────────────────────────────────────────────────

	private async Task RunIntro()
	{
		SetState(BattleState.Intro);
		ShowDialogText($"* {_enemy?.DisplayName ?? "???"} appeared!");
		await ToSignal(GetTree().CreateTimer(0.7f), SceneTreeTimer.SignalName.Timeout);
		SetState(BattleState.PlayerTurn);
	}

	// ── Fight — RhythmStrike ──────────────────────────────────────────

	private void OnFightSelected()
	{
		GD.Print("[BattleScene] STRIKE selected.");
		_actionMenu.Visible = false;
		SetState(BattleState.StrikePhase);
		ShowDialogText("* Press on the beat!");
		_rhythmStrike.Visible = true;
		_rhythmStrike.Activate();
	}

	private void OnStrikeResolved(int gradeInt)
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
		ShowDialogText($"* {hitLabel}\n* {_enemy?.DisplayName ?? "???"} took {damage} damage.");

		if (_enemyCurrentHp <= 0)
			_ = HandleVictory(killed: true);
		else
			_ = RunEnemyTurn();
	}

	// ── Perform (Bard Skills) ─────────────────────────────────────────

	private void OnPerformSelected()
	{
		GD.Print("[BattleScene] PERFORM selected");
		_subMenuMode = SubMenuMode.Perform;
		_actionMenu.Visible = false;

		// Use enemy's BardicActOptions if set, otherwise default skills
		string[] options = _enemy?.BardicActOptions is { Length: > 0 }
			? _enemy.BardicActOptions
			: DefaultBardSkillNames;

		_subMenu.Populate(options);
		_subMenu.Visible = true;
	}

	// ── Item ──────────────────────────────────────────────────────────

	private void OnItemSelected()
	{
		GD.Print("[BattleScene] Item selected");
		var inv = GameManager.Instance.InventoryItemPaths;
		if (inv.Count == 0)
		{
			ShowDialogText("* You have no items.");
			_ = RunEnemyTurn();
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

	private void OnSubMenuOptionSelected(int index)
	{
		_subMenu.Visible = false;

		if (_subMenuMode == SubMenuMode.Mercy) { HandleMercyOption(index); return; }
		if (_subMenuMode == SubMenuMode.Items)  { HandleItemOption(index);  return; }

		// Perform mode — launch the Bard skill minigame
		GD.Print($"[BattleScene] Skill option {index} selected");
		_currentSkillIndex = index;

		// Check if this option maps to "Charm" (opens CharmMinigame)
		string[] options = _enemy?.BardicActOptions is { Length: > 0 }
			? _enemy.BardicActOptions
			: DefaultBardSkillNames;

		if (index < options.Length && options[index] == "Charm")
		{
			SetState(BattleState.SkillPhase);
			_charmMinigame.Activate();
			return;
		}

		// Map option index to Bard skill
		int skillIndex = index < _bardSkills.Length ? index : 0;
		_currentSkillIndex = skillIndex;
		SetState(BattleState.SkillPhase);
		_bardSkills[skillIndex].Activate();
	}

	private void HandleMercyOption(int index)
	{
		if (index == 0)
		{
			if (_enemyCanBeSpared)
				_ = HandleVictory(killed: false);
			else
			{
				ShowDialogText("* You show mercy.\n* But it doesn't seem to care.");
				_ = RunEnemyTurn();
			}
		}
		else
		{
			bool escaped = GD.RandRange(0, 1) == 0;
			if (escaped) _ = HandleFlee();
			else
			{
				ShowDialogText("* But you couldn't get away!");
				_ = RunEnemyTurn();
			}
		}
	}

	private void OnActDialogEnded()
	{
		GameManager.Instance.SetState(GameState.Battle);
		_ = RunEnemyTurn();
	}

	private void OnSkillCompleted(int gradeInt)
	{
		var grade = (HitGrade)gradeInt;

		// Apply mercy based on enemy's SkillMercyValues (default 20 if not set)
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
		ShowDialogText($"* {result}");
		if (_enemyCanBeSpared) ShowDialogText($"* {result}\n* You can spare them now!");

		GD.Print($"[BattleScene] Skill grade={grade}, mercy+{mercyGain} → {_mercyPercent}%");
		_ = RunEnemyTurn();
	}

	private void OnCharmCompleted(int successCount, int totalNotes)
	{
		float ratio = totalNotes > 0 ? (float)successCount / totalNotes : 0f;
		var grade = ratio >= 0.75f ? HitGrade.Perfect : ratio >= 0.5f ? HitGrade.Good : HitGrade.Miss;

		int baseMercy = 30; // Charm gives higher base mercy
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
		ShowDialogText($"* {result} ({successCount}/{totalNotes} notes)");
		if (_enemyCanBeSpared) ShowDialogText($"* {result} ({successCount}/{totalNotes} notes)\n* You can spare them now!");

		GD.Print($"[BattleScene] Charm {successCount}/{totalNotes}, grade={grade}, mercy+{mercyGain} → {_mercyPercent}%");
		_ = RunEnemyTurn();
	}

	private void HandleItemOption(int index)
	{
		var inv = GameManager.Instance.InventoryItemPaths;
		if (index >= inv.Count) { ShowDialogText("* Nothing happened."); _ = RunEnemyTurn(); return; }

		string path = inv[index];
		var item = ResourceLoader.Exists(path) ? GD.Load<ItemData>(path) : null;
		if (item == null) { ShowDialogText("* That item seems to have vanished."); _ = RunEnemyTurn(); return; }

		GameManager.Instance.RemoveItem(path);
		GameManager.Instance.HealPlayer(item.HealAmount);
		ShowDialogText($"* Used {item.DisplayName}.\n* Restored {item.HealAmount} HP.");
		_ = RunEnemyTurn();
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

			ShowDialogText(line);
			await ToSignal(GetTree().CreateTimer(0.7f), SceneTreeTimer.SignalName.Timeout);
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
			ShowDialogText($"* You won!\n* Got {gold} G and {exp} EXP.\n* LV {GameManager.Instance.Love}\n* {_performance.GetSummaryText()}");
		}
		else
		{
			GameManager.Instance.RegisterSpare();
			ShowDialogText("* The enemy was spared.");
		}

		await ToSignal(GetTree().CreateTimer(1.2f), SceneTreeTimer.SignalName.Timeout);
		await ReturnToOverworld();
	}

	private async Task HandleFlee()
	{
		SetState(BattleState.Victory);
		RhythmClock.Instance.Stop();
		AudioManager.Instance.StopBgm(fadeTime: 0.5f);
		ShowDialogText("* You got away safely!");
		await ToSignal(GetTree().CreateTimer(0.8f), SceneTreeTimer.SignalName.Timeout);
		await ReturnToOverworld();
	}

	private async Task HandleDefeat()
	{
		SetState(BattleState.Defeat);
		RhythmClock.Instance.Stop();
		AudioManager.Instance.StopBgm(fadeTime: 1.0f);
		ShowDialogText("* The music fades.\n\n* GAME OVER");
		await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
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
