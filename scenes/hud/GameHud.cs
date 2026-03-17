using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Overworld HP display shown in the bottom-left corner.
/// Instantiated by OverworldBase._Ready() so every map gets it automatically.
/// </summary>
public partial class GameHud : CanvasLayer
{
	private const float BarMaxWidth = 160f;

	private Label     _nameLabel = null!;
	private Label     _hpLabel   = null!;
	private ColorRect _hpBar     = null!;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("Panel/VBox/NameRow/NameLabel");
		_hpLabel   = GetNode<Label>("Panel/VBox/HpRow/HpLabel");
		_hpBar     = GetNode<ColorRect>("Panel/VBox/HpBarBg/HpBar");

		GameManager.Instance.PlayerStatsChanged += UpdateDisplay;
		UpdateDisplay();
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PlayerStatsChanged -= UpdateDisplay;
	}

	private void UpdateDisplay()
	{
		var stats = GameManager.Instance.PlayerStats;
		_nameLabel.Text = " " + GameManager.Instance.PlayerName;
		_hpLabel.Text = $"HP  {stats.CurrentHp} / {stats.MaxHp}";
		float ratio = stats.MaxHp > 0 ? (float)stats.CurrentHp / stats.MaxHp : 0f;
		_hpBar.Size = new Vector2(Mathf.Max(0f, BarMaxWidth * ratio), _hpBar.Size.Y);
	}
}
