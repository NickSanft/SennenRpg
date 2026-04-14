using Godot;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Shown below the enemy sprite during player turn and enemy turn.
/// Displays the enemy name and any active status icons.
/// </summary>
public partial class EnemyNameplate : Control
{
	private Label _nameLabel   = null!;
	private StatusIconStrip _statusStrip = null!;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("VBox/NameLabel");

		// Status strip created dynamically — no .tscn edit required.
		_statusStrip = new StatusIconStrip
		{
			Alignment = BoxContainer.AlignmentMode.Center,
		};
		GetNode<VBoxContainer>("VBox").AddChild(_statusStrip);
	}

	public void Setup(string enemyName)
	{
		_nameLabel.Text = enemyName;
		_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
		Core.Data.UiTheme.ApplyPixelFontToAll(this);
	}

	/// <summary>Updates the status icon row beneath the enemy name.</summary>
	public void UpdateStatuses(Dictionary<StatusEffect, int> statuses)
	{
		_statusStrip.SetStatuses(statuses);
	}
}
