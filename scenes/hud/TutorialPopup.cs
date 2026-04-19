using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Hud;

/// <summary>
/// Modal tutorial popup shown the first time the player encounters a mechanic.
/// Centered panel, dims the rest of the screen, pauses the game while visible,
/// and offers a "Don't show tutorials" shortcut that flips the SkipTutorials setting.
/// Emits <see cref="ClosedEventHandler"/> and self-frees on dismiss.
/// </summary>
public partial class TutorialPopup : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private readonly Tutorial _tutorial;
    private bool _closed;

    public TutorialPopup(Tutorial tutorial)
    {
        _tutorial = tutorial;
    }

    public override void _Ready()
    {
        Layer = 55;
        // Keep popup responsive even when we pause the tree for combat tutorials.
        ProcessMode = Node.ProcessModeEnum.Always;

        var pixelFont = UiTheme.LoadPixelFont();

        // ── Screen dim ──────────────────────────────────────────────
        var dim = new ColorRect
        {
            Color        = new Color(0f, 0f, 0f, 0.55f),
            AnchorRight  = 1f,
            AnchorBottom = 1f,
            MouseFilter  = Control.MouseFilterEnum.Stop, // swallow clicks
        };
        AddChild(dim);

        // ── Centered panel ──────────────────────────────────────────
        var panel = new PanelContainer
        {
            AnchorLeft   = 0.5f,
            AnchorTop    = 0.5f,
            AnchorRight  = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft   = -170f,
            OffsetTop    = -110f,
            OffsetRight  = 170f,
            OffsetBottom = 110f,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical   = Control.GrowDirection.Both,
        };
        UiTheme.ApplyPanelTheme(panel);
        AddChild(panel);

        var vbox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
        };
        vbox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(vbox);

        // Badge line above the title
        var badge = new Label
        {
            Text                = "TUTORIAL",
            HorizontalAlignment = HorizontalAlignment.Center,
            LabelSettings       = new LabelSettings
            {
                Font         = pixelFont,
                FontSize     = 8,
                FontColor    = UiTheme.SubtleGrey,
                OutlineSize  = 1,
                OutlineColor = Colors.Black,
            },
        };
        vbox.AddChild(badge);

        // Title
        var titleLabel = new Label
        {
            Text                = _tutorial.Title,
            HorizontalAlignment = HorizontalAlignment.Center,
            LabelSettings       = new LabelSettings
            {
                Font         = pixelFont,
                FontSize     = 14,
                FontColor    = UiTheme.Gold,
                OutlineSize  = 2,
                OutlineColor = Colors.Black,
            },
        };
        vbox.AddChild(titleLabel);

        // Separator spacer
        var spacer = new Control { CustomMinimumSize = new Vector2(0, 4) };
        vbox.AddChild(spacer);

        // Body
        var bodyLabel = new Label
        {
            Text                = _tutorial.Body,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode        = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical   = Control.SizeFlags.ExpandFill,
            LabelSettings       = new LabelSettings
            {
                Font         = pixelFont,
                FontSize     = 9,
                FontColor    = Colors.White,
                OutlineSize  = 1,
                OutlineColor = Colors.Black,
            },
        };
        vbox.AddChild(bodyLabel);

        // ── Buttons row ─────────────────────────────────────────────
        var buttonRow = new HBoxContainer
        {
            Alignment           = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        buttonRow.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(buttonRow);

        var gotItButton = new Button { Text = "[Z] Got it" };
        UiTheme.ApplyButtonTheme(gotItButton);
        gotItButton.Pressed += Close;
        buttonRow.AddChild(gotItButton);

        var skipAllButton = new Button { Text = "Skip all tutorials" };
        UiTheme.ApplyButtonTheme(skipAllButton);
        skipAllButton.Pressed += OnSkipAllPressed;
        buttonRow.AddChild(skipAllButton);

        // Focus the primary button so gamepad/keyboard works immediately.
        gotItButton.CallDeferred(Control.MethodName.GrabFocus);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_closed) return;
        if (@event.IsActionPressed("interact") || @event.IsActionPressed("cancel"))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
    }

    private void OnSkipAllPressed()
    {
        var sm = SettingsManager.Instance;
        if (sm != null)
            sm.Apply(sm.Current with { SkipTutorials = true });
        Close();
    }

    private void Close()
    {
        if (_closed) return;
        _closed = true;
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        EmitSignal(SignalName.Closed);
        QueueFree();
    }
}
