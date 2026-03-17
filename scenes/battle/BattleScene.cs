using Godot;
using System.Threading.Tasks;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

public enum BattleState { Intro, PlayerTurn, EnemyTurn, DodgePhase, Victory, Defeat }

/// <summary>
/// Root battle scene state machine.
/// Phase 13: FightBar timing minigame, Act mercy system, Spare/Flee under Mercy.
/// </summary>
public partial class BattleScene : Node2D
{
	private enum SubMenuMode { Act, Mercy }

	private BattleState _state;
	private EnemyData?  _enemy;
	private int         _enemyCurrentHp;
	private bool        _enemyCanBeSpared;
	private int         _mercyPercent;
	private SubMenuMode _subMenuMode;

	private Node2D     _enemyArea   = null!;
	private Label      _dialogLabel = null!;
	private ActionMenu _actionMenu  = null!;
	private SubMenu    _subMenu     = null!;
	private BattleHUD  _battleHud   = null!;
	private DodgeBox   _dodgeBox    = null!;
	private FightBar   _fightBar    = null!;
	private Node2D     _enemyVisual = null!; // Sprite2D or Polygon2D placeholder

	public override void _Ready()
	{
		_enemyArea   = GetNode<Node2D>("EnemyArea");
		_dialogLabel = GetNode<Label>("DialogBox/DialogLabel");
		_actionMenu  = GetNode<ActionMenu>("ActionMenu");
		_subMenu     = GetNode<SubMenu>("SubMenu");
		_battleHud   = GetNode<BattleHUD>("BattleHUD");
		_dodgeBox    = GetNode<DodgeBox>("DodgeBox");
		_fightBar    = GetNode<FightBar>("FightBar");

		_actionMenu.FightSelected += OnFightSelected;
		_actionMenu.ActSelected   += OnActSelected;
		_actionMenu.ItemSelected  += OnItemSelected;
		_actionMenu.MercySelected += OnMercySelected;
		_subMenu.OptionSelected   += OnSubMenuOptionSelected;
		_subMenu.Cancelled        += OnSubMenuCancelled;
		_fightBar.Confirmed       += OnFightBarConfirmed;

		_dodgeBox.PhaseEnded += OnDodgePhaseEnded;
		var soul = _dodgeBox.GetNodeOrNull<Soul>("Soul");
		if (soul != null)
			soul.Died += OnPlayerDied;

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
		if (_enemy?.BattleSprite != null)
		{
			var sprite = new Sprite2D();
			sprite.Texture = _enemy.BattleSprite;
			_enemyVisual = sprite;
		}
		else
		{
			// Colored polygon placeholder — replaced when real art is added to EnemyData
			var poly = new Polygon2D();
			poly.Polygon = [
				new Vector2(-20, -28), new Vector2(20, -28),
				new Vector2(20,  28),  new Vector2(-20, 28)
			];
			poly.Color = new Color(0.55f, 0.3f, 0.85f, 1f); // soft purple
			_enemyVisual = poly;
		}

		_enemyArea.AddChild(_enemyVisual);

		// Idle bob — works on both Sprite2D and Polygon2D
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
		_actionMenu.Visible = newState == BattleState.PlayerTurn;
		_subMenu.Visible    = false;
		_fightBar.Visible   = false;
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
		ShowDialogText($"* {_enemy?.DisplayName ?? "???"} appeared!");
		await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
		SetState(BattleState.PlayerTurn);
	}

	// ── Action handlers ───────────────────────────────────────────────

	private void OnFightSelected()
	{
		GD.Print("[BattleScene] Fight selected — showing FightBar.");
		_actionMenu.Visible = false;
		ShowDialogText("* Choose the moment!");
		_fightBar.Visible = true;
		_fightBar.Activate();
	}

	private void OnFightBarConfirmed(float accuracy)
	{
		_fightBar.Visible = false;

		int atk  = GameManager.Instance.PlayerStats.Attack;
		int def  = _enemy?.Stats?.Defense ?? 0;
		float mult  = 1.0f + accuracy * 0.5f;  // 1.0× to 1.5× based on accuracy
		int damage  = Mathf.Max(1, Mathf.RoundToInt(atk * mult) - def);
		_enemyCurrentHp -= damage;

		string hitLabel = accuracy > 0.8f ? "Critical!" : accuracy > 0.4f ? "Hit!" : "Weak hit.";
		GD.Print($"[BattleScene] {hitLabel} Accuracy {accuracy:F2}, mult {mult:F2}, damage {damage}. Enemy HP: {_enemyCurrentHp}");
		ShowDialogText($"* {hitLabel}\n* {_enemy?.DisplayName ?? "???"} took {damage} damage.");

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
			_subMenuMode = SubMenuMode.Act;
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
		ShowDialogText("* You have no items.");
		_ = RunEnemyTurn();
	}

	private void OnMercySelected()
	{
		GD.Print("[BattleScene] Mercy selected");
		_subMenuMode = SubMenuMode.Mercy;
		_actionMenu.Visible = false;

		string spareLabel = _enemyCanBeSpared ? "Spare" : $"Spare ({_mercyPercent}%)";
		_subMenu.Populate([spareLabel, "Flee"]);
		_subMenu.Visible = true;
	}

	private void OnSubMenuOptionSelected(int index)
	{
		_subMenu.Visible = false;

		if (_subMenuMode == SubMenuMode.Mercy)
		{
			HandleMercyOption(index);
			return;
		}

		// Act mode
		GD.Print($"[BattleScene] Act option {index} selected");

		// "Check" always shows stats, regardless of array data
		if (_enemy?.ActOptions[index] == "Check")
		{
			string check = $"* {_enemy.DisplayName}\n* {_enemy.FlavorText}\n* ATK {_enemy.Stats?.Attack ?? 0}  DEF {_enemy.Stats?.Defense ?? 0}";
			ShowDialogText(check);
			_ = RunEnemyTurn();
			return;
		}

		// Apply mercy value for this act
		if (_enemy?.ActMercyValues is { Length: > 0 } && index < _enemy.ActMercyValues.Length)
			_mercyPercent = Mathf.Clamp(_mercyPercent + _enemy.ActMercyValues[index], 0, 100);

		_enemyCanBeSpared = _mercyPercent >= 100 && (_enemy?.CanBeSpared ?? false);

		// Show result text
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
		if (index == 0) // Spare
		{
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
		else // Flee
		{
			bool escaped = GD.RandRange(0, 1) == 0; // 50% chance
			GD.Print($"[BattleScene] Flee attempt. Escaped: {escaped}");
			if (escaped)
				_ = HandleFlee();
			else
			{
				ShowDialogText("* But you couldn't get away!");
				_ = RunEnemyTurn();
			}
		}
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

		string line;
		if (_enemy?.BattleDialogLines is { Length: > 0 })
			line = _enemy.BattleDialogLines[GD.RandRange(0, _enemy.BattleDialogLines.Length - 1)];
		else
			line = $"* {_enemy?.DisplayName ?? "???"} prepares to attack...";

		ShowDialogText(line);
		await ToSignal(GetTree().CreateTimer(1.2f), SceneTreeTimer.SignalName.Timeout);

		BeginDodgePhase();
	}

	private void BeginDodgePhase()
	{
		SetState(BattleState.DodgePhase);
		_dodgeBox.Visible = true;

		if (_enemy?.AttackPatternScene != null)
		{
			var pattern = _enemy.AttackPatternScene.Instantiate();
			_dodgeBox.BulletContainer.AddChild(pattern);
			GD.Print($"[BattleScene] Pattern spawned: {_enemy.AttackPatternScene.ResourcePath}");
		}
		else
		{
			GD.Print("[BattleScene] No attack pattern — empty dodge phase.");
		}

		_dodgeBox.StartPhase(3.0f);
	}

	private void OnDodgePhaseEnded()
	{
		GD.Print("[BattleScene] Dodge phase ended.");
		_dodgeBox.Visible = false;
		foreach (Node child in _dodgeBox.BulletContainer.GetChildren())
			child.QueueFree();
		SetState(BattleState.PlayerTurn);
	}

	private void OnPlayerDied()
	{
		GD.Print("[BattleScene] Player died — defeat.");
		_dodgeBox.Visible = false;
		_ = HandleDefeat();
	}

	// ── Victory / Defeat / Flee ───────────────────────────────────────

	private async Task HandleVictory(bool killed)
	{
		SetState(BattleState.Victory);

		if (killed)
		{
			GameManager.Instance.RegisterKill();
			GameManager.Instance.AddGold(_enemy?.GoldDrop ?? 0);
			ShowDialogText($"* You won!\n* Got {_enemy?.GoldDrop ?? 0} G.  LV {GameManager.Instance.Love}");
		}
		else
		{
			GameManager.Instance.RegisterSpare();
			ShowDialogText("* The enemy was spared.");
		}

		await ToSignal(GetTree().CreateTimer(2.5f), SceneTreeTimer.SignalName.Timeout);
		await ReturnToOverworld();
	}

	private async Task HandleFlee()
	{
		SetState(BattleState.Victory);
		ShowDialogText("* You got away safely!");
		await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);
		await ReturnToOverworld();
	}

	private async Task HandleDefeat()
	{
		SetState(BattleState.Defeat);
		ShowDialogText("* You feel your sins crawling on your back.\n\n* GAME OVER");
		await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);
		await SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
	}

	private async Task ReturnToOverworld()
	{
		string map = GameManager.Instance.LastMapPath;
		if (string.IsNullOrEmpty(map))
			map = "res://scenes/overworld/TestRoom.tscn";
		GD.Print($"[BattleScene] Returning to {map}");
		await SceneTransition.Instance.GoToAsync(map);
	}
}
