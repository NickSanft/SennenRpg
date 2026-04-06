using Godot;
using SennenRpg.Autoloads;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Briefly displays the current area name when a map loads.
/// Fades in, holds, then fades out automatically.
/// Added by OverworldBase after every scene load — not persistent across scenes.
/// </summary>
public partial class AreaNameLabel : CanvasLayer
{
	private Label     _label = null!;
	private ColorRect _bg    = null!;

	private const float FadeInSec  = 0.5f;
	private const float HoldSec    = 1.5f;
	private const float FadeOutSec = 1.0f;
	private const string ChimeSfx  = "res://assets/audio/sfx/area_chime.wav";

	public override void _Ready()
	{
		Layer = 3; // just above GameHud (2)
		_label = GetNode<Label>("Label");
		_label.Modulate = Colors.Transparent;

		// SNES theme: pixel font + outline
		var font = Core.Data.UiTheme.LoadPixelFont();
		if (font != null)
			_label.AddThemeFontOverride("font", font);
		_label.AddThemeConstantOverride("outline_size", 2);
		_label.AddThemeColorOverride("font_outline_color", Colors.Black);
		_label.AddThemeColorOverride("font_color", Core.Data.UiTheme.Gold);

		// Semi-transparent background behind text
		_bg = new ColorRect
		{
			Color     = Core.Data.UiTheme.PanelBg with { A = 0.8f },
			AnchorLeft  = 0.5f, AnchorRight = 0.5f,
			AnchorTop   = 0.8f, AnchorBottom = 0.8f,
			OffsetLeft  = -210f, OffsetRight  = 210f,
			OffsetTop   = -24f,  OffsetBottom = 8f,
			GrowHorizontal = Control.GrowDirection.Both,
			Modulate    = Colors.Transparent,
		};
		AddChild(_bg);
		// Move label in front of bg
		MoveChild(_bg, 0);
	}

	/// <summary>Displays an arbitrary string and plays the fade sequence.</summary>
	public void Show(string text)
	{
		if (string.IsNullOrEmpty(text)) return;
		_label.Text = text;
		PlayFade();
	}

	/// <summary>
	/// Converts a snake_case MapId to a title-cased display string and plays the fade sequence.
	/// Silently does nothing if mapId is null or empty.
	/// </summary>
	public void ShowAreaName(string mapId)
	{
		if (string.IsNullOrEmpty(mapId)) return;

		_label.Text = ToDisplayName(mapId);
		PlayFade();
	}

	private void PlayFade()
	{
		_label.Modulate = Colors.Transparent;
		_bg.Modulate    = Colors.Transparent;

		AudioManager.Instance?.PlaySfx(ChimeSfx);

		var tween = CreateTween();
		tween.TweenProperty(_label, "modulate", Colors.White, FadeInSec)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.Parallel().TweenProperty(_bg, "modulate", Colors.White, FadeInSec)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
		tween.TweenInterval(HoldSec);
		tween.TweenProperty(_label, "modulate", Colors.Transparent, FadeOutSec);
		tween.Parallel().TweenProperty(_bg, "modulate", Colors.Transparent, FadeOutSec);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	/// <summary>Converts "test_room" → "Test Room".</summary>
	private static string ToDisplayName(string mapId)
		=> System.Globalization.CultureInfo.InvariantCulture
			.TextInfo.ToTitleCase(mapId.Replace('_', ' '));
}
