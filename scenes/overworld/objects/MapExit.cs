using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Interfaces;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Transitions the player to another map.
/// AutoTrigger = true  → fires on body_entered (walk-in exits, like Undertale room edges).
/// AutoTrigger = false → waits for the player to press interact (doors).
/// </summary>
public partial class MapExit : Area2D, IInteractable
{
	[Export] public string TargetMapPath { get; set; } = "";
	[Export] public string TargetSpawnId { get; set; } = "default";
	[Export] public bool AutoTrigger { get; set; } = false;

	private bool _triggered = false;
	private Label? _promptLabel;

	public override void _Ready()
	{
		if (!AutoTrigger)
		{
			AddToGroup("interactable");

			_promptLabel = new Label();
			_promptLabel.Text = "[Z] Enter";
			_promptLabel.Position = new Vector2(-20, -20);
			_promptLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			_promptLabel.AddThemeFontSizeOverride("font_size", 8);
			_promptLabel.Visible = false;
			AddChild(_promptLabel);
		}

		BodyEntered += OnBodyEntered;
	}

	public void ShowPrompt() { if (_promptLabel != null) _promptLabel.Visible = true; }
	public void HidePrompt() { if (_promptLabel != null) _promptLabel.Visible = false; }

	private void OnBodyEntered(Node2D body)
	{
		if (!AutoTrigger) return;
		if (body.IsInGroup("player") || body is CharacterBody2D)
			TriggerExit();
	}

	public void Interact(Node player) => TriggerExit();

	public string GetInteractPrompt() => "Enter";

	private void TriggerExit()
	{
		if (_triggered || string.IsNullOrEmpty(TargetMapPath)) return;
		_triggered = true;

		GameManager.Instance.SetLastSpawn(TargetSpawnId);
		_ = SceneTransition.Instance.GoToAsync(TargetMapPath);
	}
}
