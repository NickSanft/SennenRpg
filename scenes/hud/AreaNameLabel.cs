using Godot;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Briefly displays the current area name when a map loads.
/// Fades in, holds, then fades out automatically.
/// Added by OverworldBase after every scene load — not persistent across scenes.
/// </summary>
public partial class AreaNameLabel : CanvasLayer
{
	private Label _label = null!;

	private const float FadeInSec  = 0.5f;
	private const float HoldSec    = 1.5f;
	private const float FadeOutSec = 1.0f;

	public override void _Ready()
	{
		Layer = 3; // just above GameHud (2)
		_label = GetNode<Label>("Label");
		_label.Modulate = Colors.Transparent;
	}

	/// <summary>
	/// Converts a snake_case MapId to a title-cased display string and plays the fade sequence.
	/// Silently does nothing if mapId is null or empty.
	/// </summary>
	public void ShowAreaName(string mapId)
	{
		if (string.IsNullOrEmpty(mapId)) return;

		_label.Text = ToDisplayName(mapId);
		_label.Modulate = Colors.Transparent;

		var tween = CreateTween();
		tween.TweenProperty(_label, "modulate", Colors.White, FadeInSec);
		tween.TweenInterval(HoldSec);
		tween.TweenProperty(_label, "modulate", Colors.Transparent, FadeOutSec);
		tween.TweenCallback(Callable.From(QueueFree)); // self-destruct when done
	}

	/// <summary>Converts "test_room" → "Test Room".</summary>
	private static string ToDisplayName(string mapId)
		=> System.Globalization.CultureInfo.InvariantCulture
			.TextInfo.ToTitleCase(mapId.Replace('_', ' '));
}
