using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Credits screen shown from the main menu. Code-built UI, ESC returns to MainMenu.
/// </summary>
public partial class CreditsMenu : Node2D
{
    private static readonly Color Gold       = new(1.0f, 0.85f, 0.1f);
    private static readonly Color LinkBlue   = new(0.4f, 0.7f, 1.0f);
    private static readonly Color SubtleGrey = new(0.55f, 0.55f, 0.55f);
    private static readonly Color BgColour   = new(0.07f, 0.07f, 0.12f, 1f);

    private bool _transitioning;

    public override void _Ready()
    {
        var canvas = new CanvasLayer { Layer = 5 };
        AddChild(canvas);

        var bg = new ColorRect
        {
            Color        = new Color(0.05f, 0.05f, 0.08f, 1f),
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        canvas.AddChild(bg);

        var centerer = new CenterContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        canvas.AddChild(centerer);

        var panelContainer = new PanelContainer
        {
            CustomMinimumSize = new Vector2(380f, 0f),
        };
        var style = new StyleBoxFlat
        {
            BgColor          = BgColour,
            BorderWidthLeft   = 1, BorderWidthRight  = 1,
            BorderWidthTop    = 1, BorderWidthBottom = 1,
            BorderColor       = new Color(0.25f, 0.25f, 0.35f),
            CornerRadiusTopLeft    = 4, CornerRadiusTopRight    = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
        };
        panelContainer.AddThemeStyleboxOverride("panel", style);
        centerer.AddChild(panelContainer);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   24);
        margin.AddThemeConstantOverride("margin_right",  24);
        margin.AddThemeConstantOverride("margin_top",    20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        panelContainer.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        // Title
        AddLabel(vbox, "CREDITS", 22, Gold, HorizontalAlignment.Center);
        vbox.AddChild(new HSeparator());

        // Game credits
        AddLabel(vbox, "Game", 14, Gold);
        AddLabel(vbox, "Nick Sanft", 12, Colors.White);
        AddLabel(vbox, "nick.sanft.com", 11, LinkBlue);
        AddLabel(vbox, "github.com/NickSanft", 11, LinkBlue);

        vbox.AddChild(new HSeparator());

        // Music credits
        AddLabel(vbox, "Music", 14, Gold);
        AddLabel(vbox, "Divora", 12, Colors.White);
        AddLabel(vbox, "divora.bandcamp.com", 11, LinkBlue);

        vbox.AddChild(new HSeparator());

        // Track listing
        AddLabel(vbox, "Soundtrack", 12, Gold);
        foreach (var (_, info) in MusicMetadata.All)
            AddLabel(vbox, $"{info.Title}  —  {info.Album}", 9, SubtleGrey);

        vbox.AddChild(new HSeparator());

        // Hint
        AddLabel(vbox, "[Esc] Back", 9, SubtleGrey, HorizontalAlignment.Center);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("ui_cancel") || e.IsActionPressed("menu"))
        {
            GetViewport().SetInputAsHandled();
            if (_transitioning) return;
            _transitioning = true;
            AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
            _ = SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
        }
    }

    private static void AddLabel(VBoxContainer parent, string text, int fontSize,
        Color color, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var lbl = new Label
        {
            Text                = text,
            HorizontalAlignment = align,
        };
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        lbl.AddThemeColorOverride("font_color", color);
        parent.AddChild(lbl);
    }
}
