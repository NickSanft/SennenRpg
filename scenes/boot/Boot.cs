using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Boot;

/// <summary>
/// First scene loaded by the engine.
/// Displays the game title and waits for any key/click before proceeding to MainMenu.
/// </summary>
public partial class Boot : Node2D
{
	private Label _pressAnyLabel = null!;
	private bool  _transitioning;

	public override void _Ready()
	{
		_pressAnyLabel = GetNode<Label>("UI/Center/VBox/PressAnyLabel");

		// Blink the prompt
		var tween = CreateTween().SetLoops();
		tween.TweenProperty(_pressAnyLabel, "modulate:a", 0.0f, 0.55f)
			 .SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(_pressAnyLabel, "modulate:a", 1.0f, 0.55f)
			 .SetTrans(Tween.TransitionType.Sine);
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (_transitioning) return;

		bool pressed = e switch
		{
			InputEventKey k         => k.Pressed && !k.Echo,
			InputEventMouseButton m => m.Pressed,
			_                       => false,
		};

		if (pressed)
		{
			_transitioning = true;
			_ = SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
		}
	}
}
