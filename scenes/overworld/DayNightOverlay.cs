using Godot;
using System.Threading.Tasks;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Full-screen colour overlay that darkens and tints blue at night.
/// Added as a child of WorldMap at runtime — no .tscn required.
/// Layer 5: above world tiles and player, below all HUD layers.
/// </summary>
public partial class DayNightOverlay : CanvasLayer
{
	private ColorRect _overlay = null!;

	private static readonly Color DayColor   = new(0.00f, 0.00f, 0.00f, 0.00f);
	private static readonly Color NightColor = new(0.05f, 0.05f, 0.25f, 0.55f);

	public override void _Ready()
	{
		Layer = 5;

		_overlay = new ColorRect
		{
			Color        = DayColor,
			AnchorRight  = 1f,
			AnchorBottom = 1f,
			MouseFilter  = Control.MouseFilterEnum.Ignore,
		};
		AddChild(_overlay);
	}

	/// <summary>Instantly applies state — call on scene load with the saved IsNight value.</summary>
	public void ApplyImmediate(bool isNight)
		=> _overlay.Color = isNight ? NightColor : DayColor;

	/// <summary>
	/// Animated transition: white flash followed by a 2.5 s crossfade to the new colour.
	/// </summary>
	public async Task AnimateTransition(bool toNight)
	{
		Color target = toNight ? NightColor : DayColor;
		var tween = CreateTween();
		tween.TweenProperty(_overlay, "color", Colors.White with { A = 0.55f }, 0.25f);
		tween.TweenProperty(_overlay, "color", target, 2.5f);
		await ToSignal(tween, Tween.SignalName.Finished);
	}
}
