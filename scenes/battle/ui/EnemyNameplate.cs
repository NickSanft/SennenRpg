using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Shown below the enemy sprite during player turn and enemy turn.
/// Displays the enemy name and mercy progress once Act actions are used.
/// </summary>
public partial class EnemyNameplate : Control
{
	private Label _nameLabel  = null!;
	private Label _mercyLabel = null!;

	public override void _Ready()
	{
		_nameLabel  = GetNode<Label>("VBox/NameLabel");
		_mercyLabel = GetNode<Label>("VBox/MercyLabel");
	}

	public void Setup(string enemyName)
	{
		_nameLabel.Text  = enemyName;
		_mercyLabel.Text = "";
	}

	public void UpdateMercy(int mercyPercent, bool canBeSpared)
	{
		if (canBeSpared)
		{
			_mercyLabel.Text = "* Spare ready!";
			_mercyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.1f));
		}
		else if (mercyPercent > 0)
		{
			_mercyLabel.Text = $"* Mercy: {mercyPercent}%";
			_mercyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 0.6f));
		}
		else
		{
			_mercyLabel.Text = "";
		}
	}
}
