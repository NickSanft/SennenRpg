using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Trophy/achievement menu opened from the PauseMenu.
/// Displays all trophies grouped by category with unlock status.
/// </summary>
public partial class TrophyMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static Color Gold       => UiTheme.Gold;
    private static Color SubtleGrey => UiTheme.SubtleGrey;

    private IReadOnlySet<string> _unlockedIds = new HashSet<string>();

    public override void _Ready()
    {
        Layer       = 52;
        Visible     = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// Open the trophy menu with the given set of unlocked trophy IDs.
    /// </summary>
    public void Open(IReadOnlySet<string> unlockedIds)
    {
        _unlockedIds = unlockedIds;
        BuildUi();
        UiTheme.ApplyPixelFontToAll(this);
        UiTheme.ApplyToAllButtons(this);
        Visible = true;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;
        if (e.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
    }

    private void Close()
    {
        AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    // ── UI build ─────────────────────────────────────────────────────────

    private void BuildUi()
    {
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();

        // Dim overlay
        var overlay = new ColorRect
        {
            Color        = UiTheme.OverlayDim,
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(overlay);

        // Centered panel
        var centerer = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(centerer);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(660f, 460f) };
        UiTheme.ApplyPanelTheme(panel);
        centerer.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   16);
        margin.AddThemeConstantOverride("margin_right",  16);
        margin.AddThemeConstantOverride("margin_top",    12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var rootVbox = new VBoxContainer();
        rootVbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(rootVbox);

        // Title with progress counter
        int totalUnlocked = 0;
        foreach (var t in TrophyRegistry.All)
            if (_unlockedIds.Contains(t.Id)) totalUnlocked++;

        var title = new Label
        {
            Text                = $"TROPHIES    ({totalUnlocked}/{TrophyRegistry.All.Length})",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        rootVbox.AddChild(title);

        rootVbox.AddChild(new HSeparator());

        // Scrollable body
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical    = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal  = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        rootVbox.AddChild(scroll);

        var bodyVbox = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        bodyVbox.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(bodyVbox);

        // Group by category
        var categories = TrophyLogic.CountByCategory(_unlockedIds);
        foreach (var cat in System.Enum.GetValues<TrophyCategory>())
        {
            var (catUnlocked, catTotal) = categories[cat];
            var catHeader = new Label
            {
                Text     = $"── {cat.ToString().ToUpperInvariant()}  ({catUnlocked}/{catTotal}) ──",
                Modulate = Gold,
            };
            catHeader.AddThemeFontSizeOverride("font_size", 14);
            bodyVbox.AddChild(catHeader);

            foreach (var trophy in TrophyRegistry.All)
            {
                if (trophy.Category != cat) continue;
                bool unlocked = _unlockedIds.Contains(trophy.Id);
                var (displayName, displayDesc) = TrophyLogic.DisplayInfo(trophy, unlocked);

                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 8);
                bodyVbox.AddChild(row);

                // Icon letter
                var icon = new Label
                {
                    Text     = unlocked ? trophy.IconLetter : "?",
                    Modulate = unlocked ? Gold : SubtleGrey,
                    CustomMinimumSize = new Vector2(24f, 0f),
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                icon.AddThemeFontSizeOverride("font_size", 14);
                row.AddChild(icon);

                // Name + description
                var textVbox = new VBoxContainer
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                };
                textVbox.AddThemeConstantOverride("separation", 2);
                row.AddChild(textVbox);

                var nameLabel = new Label
                {
                    Text     = displayName,
                    Modulate = unlocked ? Colors.White : SubtleGrey,
                };
                nameLabel.AddThemeFontSizeOverride("font_size", 12);
                textVbox.AddChild(nameLabel);

                var descLabel = new Label
                {
                    Text         = displayDesc,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    Modulate     = unlocked ? new Color(0.8f, 0.8f, 0.8f) : SubtleGrey,
                };
                descLabel.AddThemeFontSizeOverride("font_size", 10);
                textVbox.AddChild(descLabel);
            }

            bodyVbox.AddChild(new HSeparator());
        }

        // Bottom bar
        rootVbox.AddChild(new HSeparator());

        var progressLabel = new Label
        {
            Text                = $"{totalUnlocked}/{TrophyRegistry.All.Length} Unlocked",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        progressLabel.AddThemeFontSizeOverride("font_size", 12);
        rootVbox.AddChild(progressLabel);

        var hint = new Label
        {
            Text                = "[Esc] Back",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 12);
        rootVbox.AddChild(hint);
    }
}
