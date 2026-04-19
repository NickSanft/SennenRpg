using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Small notification popup that slides in from the top-right when a trophy
/// is unlocked. Auto-frees after the animation completes.
///
/// Usage:
///   var popup = new TrophyPopup();
///   GetTree().Root.AddChild(popup);
///   popup.Show(trophy);
/// </summary>
public partial class TrophyPopup : CanvasLayer
{
    private const float SlideInDuration  = 0.4f;
    private const float DisplayDuration  = 3.0f;
    private const float SlideOutDuration = 0.4f;
    private const float PanelWidth       = 280f;
    private const float PanelHeight      = 56f;
    private const float MarginRight      = 12f;
    private const float MarginTop        = 12f;

    private PanelContainer? _panel;

    public override void _Ready()
    {
        Layer = 55;
    }

    /// <summary>Show the trophy unlock notification and start the slide animation.</summary>
    public void Show(Trophy trophy)
    {
        var viewSize = GetViewport().GetVisibleRect().Size;

        _panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(PanelWidth, PanelHeight),
        };
        UiTheme.ApplyPanelTheme(_panel);

        // Start off-screen to the right
        float startX = viewSize.X + PanelWidth;
        float endX   = viewSize.X - PanelWidth - MarginRight;
        float y      = MarginTop;
        _panel.Position = new Vector2(startX, y);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   12);
        margin.AddThemeConstantOverride("margin_right",  12);
        margin.AddThemeConstantOverride("margin_top",    8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        _panel.AddChild(margin);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(hbox);

        // Icon letter
        var icon = new Label
        {
            Text     = trophy.IconLetter,
            Modulate = UiTheme.Gold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        icon.AddThemeFontSizeOverride("font_size", 18);
        hbox.AddChild(icon);

        // Name + "Trophy Unlocked"
        var textVbox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        textVbox.AddThemeConstantOverride("separation", 2);
        hbox.AddChild(textVbox);

        var headerLabel = new Label
        {
            Text     = "Trophy Unlocked!",
            Modulate = UiTheme.Gold,
        };
        headerLabel.AddThemeFontSizeOverride("font_size", 10);
        textVbox.AddChild(headerLabel);

        var nameLabel = new Label
        {
            Text     = trophy.DisplayName,
            Modulate = Colors.White,
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        textVbox.AddChild(nameLabel);

        AddChild(_panel);
        UiTheme.ApplyPixelFontToAll(this);

        // Slide in -> hold -> slide out -> free
        var tween = CreateTween();
        tween.TweenProperty(_panel, "position:x", endX, SlideInDuration)
             .SetTrans(Tween.TransitionType.Back)
             .SetEase(Tween.EaseType.Out);
        tween.TweenInterval(DisplayDuration);
        tween.TweenProperty(_panel, "position:x", startX, SlideOutDuration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
