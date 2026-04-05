using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Stylish top-left popup that shows the current track title and artist.
/// Fades in, holds, fades out, then self-destructs.
/// Spawned by AudioManager when the BGM track changes.
/// </summary>
public partial class NowPlayingPopup : CanvasLayer
{
	private const float FadeInSec  = 0.5f;
	private const float HoldSec    = 4.0f;
	private const float FadeOutSec = 1.0f;

	private static Color Gold       => UiTheme.Gold;
	private static Color SubtleGrey => UiTheme.SubtleGrey;

	private Label           _titleLabel      = null!;
	private Label           _artistLabel     = null!;
	private ColorRect       _bg              = null!;
	private PanelContainer? _panelContainer;

	public override void _Ready()
	{
		Layer = 3;
		BuildUI();
		UiTheme.ApplyPixelFontToAll(this);
	}

	private void BuildUI()
	{
		// Auto-sizing panel in top-left corner
		var panelContainer = new PanelContainer
		{
			AnchorLeft  = 0f,
			AnchorTop   = 0f,
			OffsetLeft  = 12f,
			OffsetTop   = 12f,
			Modulate    = Colors.Transparent,
		};
		UiTheme.ApplyPanelTheme(panelContainer);
		AddChild(panelContainer);
		// Store for fade animation
		_bg = new ColorRect { Visible = false }; // dummy — fade targets panelContainer
		AddChild(_bg);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left",   10);
		margin.AddThemeConstantOverride("margin_right",  10);
		margin.AddThemeConstantOverride("margin_top",    6);
		margin.AddThemeConstantOverride("margin_bottom", 6);
		panelContainer.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 2);
		margin.AddChild(vbox);

		// We'll fade the panelContainer instead of the old _bg
		_bg.QueueFree();
		_bg = null!;
		// Store the panel for animation
		_panelContainer = panelContainer;

		_titleLabel = new Label { Text = "" };
		_titleLabel.AddThemeFontSizeOverride("font_size", 11);
		_titleLabel.AddThemeColorOverride("font_color", Gold);
		_titleLabel.AddThemeConstantOverride("outline_size", 2);
		_titleLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		vbox.AddChild(_titleLabel);

		_artistLabel = new Label { Text = "" };
		_artistLabel.AddThemeFontSizeOverride("font_size", 10);
		_artistLabel.AddThemeColorOverride("font_color", SubtleGrey);
		_artistLabel.AddThemeConstantOverride("outline_size", 1);
		_artistLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		vbox.AddChild(_artistLabel);
	}

	/// <summary>Display track info and start the fade sequence.</summary>
	public void Show(MusicTrackInfo info)
	{
		_titleLabel.Text  = info.Title;
		_artistLabel.Text = $"by {info.Artist}";
		PlayFade();
	}

	/// <summary>Display raw title/artist strings (fallback for unregistered tracks).</summary>
	public void Show(string title, string artist)
	{
		_titleLabel.Text  = title;
		_artistLabel.Text = $"by {artist}";
		PlayFade();
	}

	private void PlayFade()
	{
		var fadeTarget = (Node)(_panelContainer ?? (Node)this);
		var tween = CreateTween();

		// Fade in (animate the whole panel + labels together)
		tween.TweenProperty(fadeTarget, "modulate", Colors.White, FadeInSec)
			.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

		// Hold
		tween.TweenInterval(HoldSec);

		// Fade out
		tween.TweenProperty(fadeTarget, "modulate", Colors.Transparent, FadeOutSec);

		// Self-destruct
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
