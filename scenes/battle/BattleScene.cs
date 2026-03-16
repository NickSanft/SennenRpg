using Godot;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

public enum BattleState { Intro, PlayerTurn, EnemyTurn, DodgePhase, Victory, Defeat }

/// <summary>
/// Root of the battle scene. Owns the state machine and wires all sub-nodes.
/// Phase 11: Fight/Act/Item/Mercy functional with placeholder enemy turn.
/// Phase 12 will replace RunEnemyTurn with the real dodge phase.
/// </summary>
public partial class BattleScene : Node2D
{
	private BattleState _state;
	private EnemyData?  _enemy;
	private int         _enemyCurrentHp;
	private bool        _enemyCanBeSpared;

	private Node2D    _enemyArea   = null!;
	private Label     _dialogLabel = null!;
	private ActionMenu _actionMenu = null!;
	private SubMenu    _subMenu    = null!;
	private BattleHUD  _battleHud  = null!;
	private Sprite2D   _enemySprite = null!;

	public override void _Ready()
	{
		_enemyArea   = GetNode<Node2D>("EnemyArea");
		_dialogLabel = GetNode<Label>("DialogBox/DialogLabel");
		_actionMenu  = GetNode<ActionMenu>("ActionMenu");
		_subMenu     = GetNode<SubMenu>("SubMenu");
		_battleHud   = GetNode<BattleHUD>("BattleHUD");

		_actionMenu.FightSelected += OnFightSelected;
		_actionMenu.ActSelected   += OnActSelected;
		_actionMenu.ItemSelected  += OnItemSelected;
		_actionMenu.MercySelected += OnMercySelected;
		_subMenu.OptionSelected   += OnSubMenuOptionSelected;
		_subMenu.Cancelled        += OnSubMenuCancelled;

		// Pull the pending encounter from the registry
		var encounter = BattleRegistry.Instance.GetPendingEncounter();
		if (encounter != null && encounter.Enemies.Count > 0)
			_enemy = encounter.Enemies[0];
		else
			GD.PushWarning("[BattleScene] No pending encounter — using placeholder enemy.");

		_enemyCurrentHp = _enemy?.Stats?.MaxHp ?? 10;

		SetupEnemySprite();
		_ = RunIntro();
	}

	// ── Setup ─────────────────────────────────────────────────────────

	private void SetupEnemySprite()
	{
		_enemySprite = new Sprite2D();
		if (_enemy?.BattleSprite != null)
			_enemySprite.Texture = _enemy.BattleSprite;
		_enemyArea.AddChild(_enemySprite);

		// Idle bob: loops up and down continuously
		var tween = CreateTween().SetLoops();
		tween.TweenProperty(_enemySprite, "position:y",  4f, 0.6f)
		     .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(_enemySprite, "position:y", -4f, 0.6f)
		     .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
	}

	// ── State machine ─────────────────────────────────────────────────

	private void SetState(BattleState newState)
	{
		_state = newState;
		_actionMenu.Visible = newState == BattleState.PlayerTurn;
		_subMenu.Visible    = false;
		GD.Print($"[BattleScene] State → {newState}");

		if (newState == BattleState.PlayerTurn)
			_actionMenu.FocusFirst();
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
		string name = _enemy?.DisplayName ?? "???";
		ShowDialogText($"* {name} appeared!");
		await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
		SetState(BattleState.PlayerTurn);
	}

	// ── Action handlers ───────────────────────────────────────────────

	private void OnFightSelected()
	{
		GD.Print("[BattleScene] Fight selected");
		// Phase 13 will replace this with the FightBar timing minigame
		int atk = GameManager.Instance.PlayerStats.Attack;
		int def = _enemy?.Stats?.Defense ?? 0;
		int damage = Mathf.Max(1, atk - def);
		_enemyCurrentHp -= damage;
		GD.Print($"[BattleScene] Dealt {damage} damage. Enemy HP: {_enemyCurrentHp}");
		ShowDialogText($"* You deal {damage} damage!");

		if (_enemyCurrentHp <= 0)
			_ = HandleVictory(killed: true);
		else
			_ = RunEnemyTurn();
	}

	private void OnActSelected()
	{
		GD.Print("[BattleScene] Act selected");
		if (_enemy?.ActOptions is { Length: > 0 })
		{
			_actionMenu.Visible = false;
			_subMenu.Populate(_enemy.ActOptions);
			_subMenu.Visible = true;
		}
		else
		{
			ShowDialogText("* There's nothing useful to do.");
		}
	}

	private void OnItemSelected()
	{
		GD.Print("[BattleScene] Item selected");
		// Phase 13: inventory list — placeholder for now
		ShowDialogText("* You have no items.");
		_ = RunEnemyTurn();
	}

	private void OnMercySelected()
	{
		GD.Print("[BattleScene] Mercy selected");
		if (_enemyCanBeSpared)
		{
			_ = HandleVictory(killed: false);
		}
		else
		{
			ShowDialogText("* You show mercy.\n* But it doesn't seem to care.");
			_ = RunEnemyTurn();
		}
	}

	private void OnSubMenuOptionSelected(int index)
	{
		_subMenu.Visible = false;
		GD.Print($"[BattleScene] Act option {index} selected");
		// Phase 13: mercy value system — placeholder
		ShowDialogText($"* You try: {_enemy?.ActOptions[index] ?? "???"}.");
		_ = RunEnemyTurn();
	}

	private void OnSubMenuCancelled()
	{
		_subMenu.Visible = false;
		SetState(BattleState.PlayerTurn);
	}

	// ── Enemy turn (Phase 12 will replace with real dodge phase) ──────

	private async Task RunEnemyTurn()
	{
		SetState(BattleState.EnemyTurn);

		// Pick a random dialog line if the enemy has any
		string line;
		if (_enemy?.BattleDialogLines is { Length: > 0 })
			line = _enemy.BattleDialogLines[GD.RandRange(0, _enemy.BattleDialogLines.Length - 1)];
		else
			line = $"* {_enemy?.DisplayName ?? "???"} prepares to attack...";

		ShowDialogText(line);
		await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);

		// Placeholder — Phase 12 starts DodgeBox here
		ShowDialogText("* The attack misses!");
		await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

		SetState(BattleState.PlayerTurn);
	}

	// ── Victory / Defeat ──────────────────────────────────────────────

	private async Task HandleVictory(bool killed)
	{
		SetState(BattleState.Victory);

		if (killed)
		{
			GameManager.Instance.RegisterKill();
			GameManager.Instance.AddGold(_enemy?.GoldDrop ?? 0);
			ShowDialogText($"* You won!\n* Got {_enemy?.GoldDrop ?? 0} G.");
		}
		else
		{
			GameManager.Instance.RegisterSpare();
			ShowDialogText("* The enemy was spared.");
		}

		await ToSignal(GetTree().CreateTimer(2.5f), SceneTreeTimer.SignalName.Timeout);

		string returnMap = GameManager.Instance.LastMapPath;
		if (string.IsNullOrEmpty(returnMap))
			returnMap = "res://scenes/overworld/TestRoom.tscn";

		GD.Print($"[BattleScene] Returning to {returnMap}");
		await SceneTransition.Instance.GoToAsync(returnMap);
	}
}
