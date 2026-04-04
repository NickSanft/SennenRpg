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
	private Label _statusLabel = null!;

	public override void _Ready()
	{
		_nameLabel = GetNode<Label>("VBox/NameLabel");

		// Status label created dynamically — no .tscn edit required.
		_statusLabel = new Label { Text = "" };
		GetNode<VBoxContainer>("VBox").AddChild(_statusLabel);
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
		if (statuses.Count == 0)
		{
			_statusLabel.Text = "";
			return;
		}
		var parts = new List<string>();
		foreach (var (effect, turns) in statuses)
			parts.Add($"{StatusLogic.IconText(effect)}({turns})");
		_statusLabel.Text = string.Join(" ", parts);
	}
}
