using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// CanvasLayer (Layer 10) that shows the player's name, LV, and HP bar
/// during battle. Always renders above the battle scene.
/// Subscribes to GameManager.PlayerStatsChanged for live HP updates.
/// </summary>
public partial class BattleHUD : CanvasLayer
{
	private Label _nameLabel = null!;
	private Label _lvLabel   = null!;
	private Label _hpLabel   = null!;
	private ColorRect _hpBarBg = null!;
	private ColorRect _hpBar   = null!;

	public override void _Ready()
	{
		Layer = 10;

		_nameLabel = GetNode<Label>("HudPanel/HBoxContainer/NameLabel");
		_lvLabel   = GetNode<Label>("HudPanel/HBoxContainer/LvLabel");
		_hpLabel   = GetNode<Label>("HudPanel/HBoxContainer/HpLabel");
		_hpBarBg   = GetNode<ColorRect>("HudPanel/HBoxContainer/HpBarBg");
		_hpBar     = GetNode<ColorRect>("HudPanel/HBoxContainer/HpBarBg/HpBar");

		GameManager.Instance.PlayerStatsChanged += UpdateHud;
		UpdateHud();
	}

	public override void _ExitTree()
	{
		// Disconnect to avoid dead-signal issues if GameManager outlives this node
		if (GameManager.Instance != null)
			GameManager.Instance.PlayerStatsChanged -= UpdateHud;
	}

	private void UpdateHud()
	{
		var stats = GameManager.Instance.PlayerStats;
		_nameLabel.Text = "* " + GameManager.Instance.PlayerName;
		_lvLabel.Text   = $"LV {GameManager.Instance.Love}";
		_hpLabel.Text   = $"{stats.CurrentHp} / {stats.MaxHp}";

		float ratio = stats.MaxHp > 0 ? (float)stats.CurrentHp / stats.MaxHp : 0f;
		_hpBar.Size = new Vector2(_hpBarBg.Size.X * ratio, _hpBarBg.Size.Y);
	}
}
