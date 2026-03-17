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

	/// <summary>Kick off the rise-and-fade animation.</summary>
	public void Play(int damage, bool isCrit)
	{
		_label.Text = damage.ToString();
		_label.AddThemeColorOverride("font_color", isCrit ? new Color(1f, 0.9f, 0f) : Colors.White);
		_label.AddThemeFontSizeOverride("font_size", isCrit ? 18 : 14);

		var tween = CreateTween();
		tween.TweenProperty(this, "position:y", Position.Y - 44f, 0.65f)
		     .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.Parallel().TweenProperty(_label, "modulate:a", 0f, 0.65f)
		     .SetDelay(0.25f);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
