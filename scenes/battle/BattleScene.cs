using Godot;
using System.Threading.Tasks;
using System.Threading;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

public enum BattleState { Intro, PlayerTurn, EnemyTurn, RhythmPhase, StrikePhase, Victory, Defeat }

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

	private void StartBattleBgm(EncounterData? encounter)
	{
		// Resolve BGM path: encounter > enemy > default
		string bgmPath = encounter?.BattleBgmPath ?? "";
		if (string.IsNullOrEmpty(bgmPath))
			bgmPath = _enemy?.BattleBgmPath ?? "";

		// Resolve BPM: encounter > enemy > default
		float bpm = (encounter?.BattleBpm ?? 0f) > 0f ? encounter!.BattleBpm
				  : (_enemy?.BattleBpm ?? 0f) > 0f    ? _enemy!.BattleBpm
				  : RhythmConstants.DefaultBpm;

		if (!string.IsNullOrEmpty(bgmPath) && ResourceLoader.Exists(bgmPath))
		{
			AudioManager.Instance.PlayBgm(bgmPath, fadeTime: 0.1f, bpm: bpm);
		}
		else
		{
			// No audio file — just configure the clock with the correct BPM
			RhythmClock.Instance.SetBpm(bpm);
			GD.Print($"[BattleScene] No battle BGM found. RhythmClock BPM set to {bpm}.");
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
		_enemyNameplate.Visible = newState is BattleState.PlayerTurn or BattleState.EnemyTurn;
		GD.Print($"[BattleScene] State → {newState}");

		if (newState == BattleState.PlayerTurn)
			_actionMenu.FocusFirst();
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

	// ── Perform (Bard Skills) — stub for Phase 5 ─────────────────────

	private void OnPerformSelected()
	{
		GD.Print("[BattleScene] PERFORM selected");
		if (_enemy?.ActOptions is { Length: > 0 })
		{
			_subMenuMode = SubMenuMode.Perform;
			_actionMenu.Visible = false;
			_subMenu.Populate(_enemy.ActOptions);
			_subMenu.Visible = true;
		}
		else
		{
			ShowDialogText("* There's nothing to perform here.");
		}
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

		// Perform mode — Phase 5 will wire full Bard skill minigames.
		// For now, fall back to the legacy Act logic so existing enemies remain functional.
		GD.Print($"[BattleScene] Perform option {index} selected");

		if (_enemy?.ActOptions[index] == "Check")
		{
			string check = $"* {_enemy.DisplayName}\n* {_enemy.FlavorText}\n* ATK {_enemy.Stats?.Attack ?? 0}  DEF {_enemy.Stats?.Defense ?? 0}";
			ShowDialogText(check);
			_ = RunEnemyTurn();
			return;
		}

		if (_enemy?.ActMercyValues is { Length: > 0 } && index < _enemy.ActMercyValues.Length)
			_mercyPercent = Mathf.Clamp(_mercyPercent + _enemy.ActMercyValues[index], 0, 100);

		_enemyCanBeSpared = _mercyPercent >= 100 && (_enemy?.CanBeSpared ?? false);
		_enemyNameplate.UpdateMercy(_mercyPercent, _enemyCanBeSpared);

		if (_enemy != null)
		{
			string optionKey = _enemy.ActOptions[index].ToLower().Replace(" ", "_");
			string actPath   = $"res://dialog/timelines/act_{_enemy.EnemyId}_{optionKey}.dtl";
			if (ResourceLoader.Exists(actPath))
			{
				GameManager.Instance.SetState(GameState.Dialog);
				DialogicBridge.Instance.ConnectTimelineEnded(
					new Callable(this, MethodName.OnActDialogEnded));
				DialogicBridge.Instance.StartTimelineWithFlags(actPath);
				return;
			}
		}

		string result;
		if (_enemy?.ActResultTexts is { Length: > 0 } && index < _enemy.ActResultTexts.Length
			&& !string.IsNullOrEmpty(_enemy.ActResultTexts[index]))
			result = $"* {_enemy.ActResultTexts[index]}";
		else
			result = $"* You try: {_enemy?.ActOptions[index] ?? "???"}";

		if (_enemyCanBeSpared)
			result += "\n* You can now spare them!";

		ShowDialogText(result);
		_ = RunEnemyTurn();
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

		if (killed)
		{
			int gold = _enemy?.GoldDrop ?? 0;
			int exp  = _enemy?.ExpDrop  ?? 0;
			GameManager.Instance.RegisterKill();
			GameManager.Instance.AddGold(gold);
			GameManager.Instance.AddExp(exp);
			ShowDialogText($"* You won!\n* Got {gold} G and {exp} EXP.\n* LV {GameManager.Instance.Love}");
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
		ShowDialogText("* You got away safely!");
		await ToSignal(GetTree().CreateTimer(0.8f), SceneTreeTimer.SignalName.Timeout);
		await ReturnToOverworld();
	}

	private async Task HandleDefeat()
	{
		SetState(BattleState.Defeat);
		RhythmClock.Instance.Stop();
		ShowDialogText("* The music fades.\n\n* GAME OVER");
		await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
		await SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
	}

	private async Task ReturnToOverworld()
	{
		string map = GameManager.Instance.LastMapPath;
		if (string.IsNullOrEmpty(map))
			map = "res://scenes/overworld/TestRoom.tscn";
		await SceneTransition.Instance.GoToAsync(map);
	}
}
