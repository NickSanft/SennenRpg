using Godot;
using System.Collections.Generic;
using System.Linq;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Full-screen jukebox menu (CanvasLayer 52) that lets the player replay any
/// BGM track they have previously heard. Opened from <see cref="SennenRpg.Scenes.Overworld.JukeboxProp"/>.
/// </summary>
public partial class JukeboxMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private string _originalBgmPath = "";
    private List<MusicTrackInfo> _tracks = new();
    private readonly List<Button> _trackButtons = new();
    private VBoxContainer _listVbox = null!;

    public override void _Ready()
    {
        Layer       = 52;
        Visible     = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void Open()
    {
        _originalBgmPath = AudioManager.Instance?.CurrentBgmPath ?? "";

        // Gather unlocked paths from GameManager. The merge step will add
        // UnlockedBgmPaths; until then, fall back to showing all known tracks.
        IReadOnlyCollection<string> unlockedPaths = GetUnlockedPaths();

        var allTracks = MusicMetadata.All.Values.ToList();
        var unlocked  = JukeboxLogic.GetUnlockedTracks(allTracks, unlockedPaths);
        _tracks       = JukeboxLogic.SortByAlbum(unlocked);

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
        QueueFree();
    }

    // ── UI construction ──────────────────────────────────────────────────

    private void BuildUi()
    {
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();

        _trackButtons.Clear();

        // Dim overlay
        var overlay = new ColorRect
        {
            Color        = UiTheme.OverlayDim,
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(overlay);

        var centerer = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(centerer);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(600f, 420f) };
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

        // Title
        var title = new Label
        {
            Text                = "JUKEBOX",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = UiTheme.Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        rootVbox.AddChild(title);

        rootVbox.AddChild(new HSeparator());

        // Scrollable track list
        var scroll = new ScrollContainer
        {
            CustomMinimumSize    = new Vector2(560f, 320f),
            SizeFlagsVertical    = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        rootVbox.AddChild(scroll);

        _listVbox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _listVbox.AddThemeConstantOverride("separation", 2);
        scroll.AddChild(_listVbox);

        // STOP button — restores original BGM
        var stopBtn = new Button
        {
            Text                = "STOP",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        stopBtn.AddThemeColorOverride("font_color", UiTheme.NeedRed);
        stopBtn.AddThemeFontSizeOverride("font_size", 12);
        stopBtn.Pressed      += OnStopPressed;
        stopBtn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
        _listVbox.AddChild(stopBtn);

        // Track buttons
        string currentBgm = AudioManager.Instance?.CurrentBgmPath ?? "";
        for (int i = 0; i < _tracks.Count; i++)
        {
            var track = _tracks[i];
            int capturedIndex = i;
            string label = $"{track.Title}  --  {track.Artist}";

            var btn = new Button
            {
                Text                = label,
                Alignment           = HorizontalAlignment.Left,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };

            bool isPlaying = track.ResourcePath == currentBgm;
            btn.AddThemeColorOverride("font_color", isPlaying ? UiTheme.Gold : Colors.White);
            btn.AddThemeFontSizeOverride("font_size", 12);
            btn.Pressed      += () => OnTrackSelected(capturedIndex);
            btn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);

            _listVbox.AddChild(btn);
            _trackButtons.Add(btn);
        }

        // Empty state
        if (_tracks.Count == 0)
        {
            var empty = new Label
            {
                Text                = "No tracks discovered yet.",
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate            = UiTheme.SubtleGrey,
            };
            empty.AddThemeFontSizeOverride("font_size", 12);
            _listVbox.AddChild(empty);
        }

        rootVbox.AddChild(new HSeparator());

        var hint = new Label
        {
            Text                = "[Esc] Back",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = UiTheme.SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 12);
        rootVbox.AddChild(hint);

        // Focus first track button (or stop button)
        if (_listVbox.GetChildCount() > 0)
        {
            var first = _listVbox.GetChild(0);
            if (first is Control c)
                c.CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    // ── Interaction ──────────────────────────────────────────────────────

    private void OnTrackSelected(int index)
    {
        if (index < 0 || index >= _tracks.Count) return;
        var track = _tracks[index];

        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        AudioManager.Instance?.PlayBgm(track.ResourcePath);

        UpdateHighlights(track.ResourcePath);
    }

    private void OnStopPressed()
    {
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);

        // Restore the original map BGM
        if (!string.IsNullOrEmpty(_originalBgmPath))
            AudioManager.Instance?.PlayBgm(_originalBgmPath);

        UpdateHighlights(_originalBgmPath);
    }

    private void UpdateHighlights(string nowPlaying)
    {
        for (int i = 0; i < _trackButtons.Count; i++)
        {
            bool active = i < _tracks.Count && _tracks[i].ResourcePath == nowPlaying;
            _trackButtons[i].RemoveThemeColorOverride("font_color");
            _trackButtons[i].AddThemeColorOverride("font_color",
                active ? UiTheme.Gold : Colors.White);
        }
    }

    /// <summary>
    /// Returns the set of BGM paths the player has heard. Uses reflection to
    /// call <c>GameManager.Instance.UnlockedBgmPaths</c> when it exists (added
    /// by the merge step); otherwise falls back to all tracks in MusicMetadata.
    /// </summary>
    private static IReadOnlyCollection<string> GetUnlockedPaths()
    {
        var gm = GameManager.Instance;
        var prop = gm.GetType().GetProperty("UnlockedBgmPaths");
        if (prop != null)
        {
            var value = prop.GetValue(gm);
            if (value is IReadOnlyCollection<string> paths)
                return paths;
        }
        // Fallback: show every known track until GameManager gains the property.
        return (IReadOnlyCollection<string>)MusicMetadata.All.Keys;
    }
}
