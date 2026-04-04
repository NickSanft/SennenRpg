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

    private Label     _titleLabel  = null!;
    private Label     _artistLabel = null!;
    private ColorRect _bg          = null!;

    public override void _Ready()
    {
        Layer = 3;
        BuildUI();
    }

    private void BuildUI()
    {
        // Background panel — top-left
        _bg = new ColorRect
        {
            Color        = UiTheme.PanelBg with { A = 0.85f },
            AnchorLeft   = 0f,
            AnchorTop    = 0f,
            OffsetLeft   = 12f,
            OffsetTop    = 12f,
            OffsetRight  = 280f,
            OffsetBottom = 56f,
            Modulate     = Colors.Transparent,
        };
        AddChild(_bg);

        var vbox = new VBoxContainer
        {
            AnchorLeft   = 0f,
            AnchorTop    = 0f,
            OffsetLeft   = 20f,
            OffsetTop    = 16f,
            OffsetRight  = 272f,
            OffsetBottom = 52f,
        };
        vbox.AddThemeConstantOverride("separation", 0);
        AddChild(vbox);

        _titleLabel = new Label
        {
            Text     = "",
            Modulate = Colors.Transparent,
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        _titleLabel.AddThemeColorOverride("font_color", Gold);
        _titleLabel.AddThemeConstantOverride("outline_size", 2);
        _titleLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        vbox.AddChild(_titleLabel);

        _artistLabel = new Label
        {
            Text     = "",
            Modulate = Colors.Transparent,
        };
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
        var tween = CreateTween();

        // Fade in
        tween.TweenProperty(_bg, "modulate", Colors.White, FadeInSec)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.Parallel().TweenProperty(_titleLabel, "modulate", Colors.White, FadeInSec)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.Parallel().TweenProperty(_artistLabel, "modulate", Colors.White, FadeInSec)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

        // Hold
        tween.TweenInterval(HoldSec);

        // Fade out
        tween.TweenProperty(_bg, "modulate", Colors.Transparent, FadeOutSec);
        tween.Parallel().TweenProperty(_titleLabel, "modulate", Colors.Transparent, FadeOutSec);
        tween.Parallel().TweenProperty(_artistLabel, "modulate", Colors.Transparent, FadeOutSec);

        // Self-destruct
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
