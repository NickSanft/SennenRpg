using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Shown when the player's HP reaches zero in battle.
/// Displays a brief message then returns to the Main Menu.
/// </summary>
public partial class GameOver : Node2D
{
	private const float LingerSec = 3.5f;

	public override void _Ready()
	{
		// Fade in the panel
		var panel = GetNodeOrNull<CanvasItem>("UI");
		if (panel != null)
		{
			panel.Modulate = Colors.Transparent;
			var tween = CreateTween();
			tween.TweenProperty(panel, "modulate", Colors.White, 1.2f)
				 .SetTrans(Tween.TransitionType.Sine);
		}

		GetTree().CreateTimer(LingerSec).Timeout +=
			() => _ = SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
	}

	public override void _UnhandledInput(InputEvent e)
	{
		// Allow skipping the wait with any key press
		if (e is InputEventKey k && k.Pressed && !k.Echo)
			_ = SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
	}
}
