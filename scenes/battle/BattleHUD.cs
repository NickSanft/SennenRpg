using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// CanvasLayer (Layer 10) that shows the player's name, LV, HP bar, and battle stats
/// during battle. Subscribes to GameManager.PlayerStatsChanged for live updates.
/// </summary>
public partial class BattleHUD : CanvasLayer
{
	private Label _nameLabel  = null!;
	private Label _lvLabel    = null!;
	private Label _hpLabel    = null!;
	private Label _statsLabel = null!;
	private ColorRect _hpBarBg = null!;
	private ColorRect _hpBar   = null!;

	public override void _Ready()
	{
		Layer = 10;

		_nameLabel  = GetNode<Label>("HudPanel/VBoxContainer/HBoxContainer/NameLabel");
		_lvLabel    = GetNode<Label>("HudPanel/VBoxContainer/HBoxContainer/LvLabel");
		_hpLabel    = GetNode<Label>("HudPanel/VBoxContainer/HBoxContainer/HpLabel");
		_hpBarBg    = GetNode<ColorRect>("HudPanel/VBoxContainer/HBoxContainer/HpBarBg");
		_hpBar      = GetNode<ColorRect>("HudPanel/VBoxContainer/HBoxContainer/HpBarBg/HpBar");
		_statsLabel = GetNode<Label>("HudPanel/VBoxContainer/StatsLabel");

		GameManager.Instance.PlayerStatsChanged += UpdateHud;
		Callable.From(UpdateHud).CallDeferred();
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PlayerStatsChanged -= UpdateHud;
	}

	private void UpdateHud()
	{
		var stats = GameManager.Instance.PlayerStats;
		_nameLabel.Text = "* " + GameManager.Instance.PlayerName;
		_lvLabel.Text   = "LV 1";
		_hpLabel.Text   = $"{stats.CurrentHp} / {stats.MaxHp}";

		float ratio = stats.MaxHp > 0 ? (float)stats.CurrentHp / stats.MaxHp : 0f;
		_hpBar.Size = new Vector2(_hpBarBg.Size.X * ratio, _hpBarBg.Size.Y);

		_statsLabel.Text =
			$"ATK:{stats.Attack}  DEF:{stats.Defense}  SPD:{stats.Speed}" +
			$"  MAG:{stats.Magic}  RES:{stats.Resistance}  LCK:{stats.Luck}";
	}
}
