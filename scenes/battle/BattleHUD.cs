using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// CanvasLayer (Layer 10) showing player name, LV, HP bar, MP bar, and battle stats.
/// Updates live via GameManager.PlayerStatsChanged.
/// </summary>
public partial class BattleHUD : CanvasLayer
{
	private Label     _nameLabel  = null!;
	private Label     _lvLabel    = null!;
	private Label     _hpLabel    = null!;
	private Label     _mpLabel    = null!;
	private Label     _statsLabel = null!;
	private ColorRect _hpBarBg    = null!;
	private ColorRect _hpBar      = null!;
	private ColorRect _mpBarBg    = null!;
	private ColorRect _mpBar      = null!;

	public override void _Ready()
	{
		Layer = 10;

		const string row = "HudPanel/VBoxContainer/HBoxContainer/";
		_nameLabel  = GetNode<Label>(row + "NameLabel");
		_lvLabel    = GetNode<Label>(row + "LvLabel");
		_hpLabel    = GetNode<Label>(row + "HpLabel");
		_hpBarBg    = GetNode<ColorRect>(row + "HpBarBg");
		_hpBar      = GetNode<ColorRect>(row + "HpBarBg/HpBar");
		_mpLabel    = GetNode<Label>(row + "MpLabel");
		_mpBarBg    = GetNode<ColorRect>(row + "MpBarBg");
		_mpBar      = GetNode<ColorRect>(row + "MpBarBg/MpBar");
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
		_lvLabel.Text   = $"LV {GameManager.Instance.PlayerLevel}";
		_hpLabel.Text   = $"{stats.CurrentHp} / {stats.MaxHp} HP";
		_mpLabel.Text   = $"{stats.CurrentMp} / {stats.MaxMp} MP";

		float hpRatio = stats.MaxHp > 0 ? (float)stats.CurrentHp / stats.MaxHp : 0f;
		_hpBar.Size = new Vector2(_hpBarBg.Size.X * hpRatio, _hpBarBg.Size.Y);

		float mpRatio = stats.MaxMp > 0 ? (float)stats.CurrentMp / stats.MaxMp : 0f;
		_mpBar.Size = new Vector2(_mpBarBg.Size.X * mpRatio, _mpBarBg.Size.Y);

		_statsLabel.Text =
			$"ATK:{stats.Attack}  DEF:{stats.Defense}  SPD:{stats.Speed}" +
			$"  MAG:{stats.Magic}  RES:{stats.Resistance}  LCK:{stats.Luck}";
	}
}
