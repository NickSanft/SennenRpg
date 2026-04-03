using Godot;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Self-managing floating damage label. Spawn via PackedScene.Instantiate(), set Position,
/// add to scene, then call Play(). Frees itself after the animation finishes.
/// </summary>
public partial class DamageNumber : Node2D
{
	private Label _label = null!;

	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
	}

	/// <summary>Kick off the rise-and-fade animation with scale pop and random offset.</summary>
	public void Play(int damage, bool isCrit)
	{
		// Random spawn offset so multiple numbers don't overlap
		Position += new Vector2(
			(float)GD.RandRange(-8.0, 8.0),
			(float)GD.RandRange(-4.0, 4.0));

		_label.Text = damage.ToString();
		_label.AddThemeColorOverride("font_color", isCrit ? new Color(1f, 0.9f, 0f) : Colors.White);
		_label.AddThemeFontSizeOverride("font_size", isCrit ? 18 : 14);

		// Text outline for readability
		_label.AddThemeConstantOverride("outline_size", 2);
		_label.AddThemeColorOverride("font_outline_color", Colors.Black);

		// Scale pop: start large, settle to 1x
		Scale = new Vector2(1.5f, 1.5f);

		var tween = CreateTween();
		// Pop-in scale
		tween.TweenProperty(this, "scale", Vector2.One, 0.15f)
		     .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
		// Rise
		tween.Parallel().TweenProperty(this, "position:y", Position.Y - 44f, 0.65f)
		     .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		// Fade
		tween.Parallel().TweenProperty(_label, "modulate:a", 0f, 0.65f)
		     .SetDelay(0.25f);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
