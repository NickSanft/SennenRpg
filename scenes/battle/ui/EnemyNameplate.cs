using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Shown below the enemy sprite during player turn and enemy turn.
/// Displays the enemy name.
/// </summary>
public partial class EnemyNameplate : Control
{
	private Label _nameLabel = null!;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("VBox/NameLabel");
	}

	public void Setup(string enemyName)
	{
		_nameLabel.Text = enemyName;
	}
}
