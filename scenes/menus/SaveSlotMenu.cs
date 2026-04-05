using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Slot-picker screen shown before New Game and Continue.
/// Set <see cref="PendingMode"/> before navigating here.
///
///   NewGame  — every slot shows "New Game" / "Overwrite"; choosing one resets state and starts fresh.
///   Continue — existing slots show "Load"; empty slots show "New Game" as a convenience shortcut.
/// </summary>
public partial class SaveSlotMenu : Node2D
{
    public enum MenuMode { NewGame, Continue }

    /// <summary>Set by the caller before transitioning to this scene.</summary>
    public static MenuMode PendingMode { get; set; } = MenuMode.Continue;

    private bool _transitioning = false;
    private CanvasLayer? _confirmOverlay;
    private int _pendingSlot;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        var canvas = new CanvasLayer { Layer = 5 };
        AddChild(canvas);

        var bg = new ColorRect
        {
            Color        = UiTheme.PanelBg,
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        canvas.AddChild(bg);

        var margin = new MarginContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        margin.AddThemeConstantOverride("margin_left",   40);
        margin.AddThemeConstantOverride("margin_right",  40);
        margin.AddThemeConstantOverride("margin_top",    20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        canvas.AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        // Title
        string title = PendingMode == MenuMode.NewGame
            ? "New Game — Choose a Slot"
            : "Continue — Choose a Slot";
        var titleLabel = new Label
        {
            Text                = title,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        root.AddChild(titleLabel);

        root.AddChild(new HSeparator());

        // Slot cards
        for (int s = 1; s <= SaveSlotLogic.MaxSlots; s++)
            BuildSlotCard(root, s);

        root.AddChild(new HSeparator());

        // Back button
        var backBtn = new Button
        {
            Text                = "Back",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            CustomMinimumSize   = new Vector2(120, 0),
        };
        backBtn.Pressed += OnBackPressed;
        backBtn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
        root.AddChild(backBtn);

        // Apply SNES theme to all buttons
        UiTheme.ApplyToAllButtons(canvas);
        UiTheme.ApplyPixelFontToAll(canvas);

        // Apply pixel font to title
        titleLabel.AddThemeColorOverride("font_color", UiTheme.Gold);
        var font = UiTheme.LoadPixelFont();
        if (font != null) titleLabel.AddThemeFontOverride("font", font);
    }

    // ── Slot card ─────────────────────────────────────────────────────────────

    private void BuildSlotCard(VBoxContainer parent, int slot)
    {
        var info = SaveManager.Instance.LoadSlotInfo(slot);

        var card = new PanelContainer();
        card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        card.AddChild(hbox);

        // Info column
        var infoCol = new VBoxContainer();
        infoCol.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        infoCol.AddThemeConstantOverride("separation", 2);
        hbox.AddChild(infoCol);

        if (info != null)
        {
            var nameLabel = new Label { Text = $"Slot {slot} — {info.PlayerName}  (Lv {info.Level} {info.ClassName})" };
            nameLabel.AddThemeFontSizeOverride("font_size", 14);

            var timeLabel = new Label
            {
                Text     = $"Play time: {SaveSlotLogic.FormatPlayTime(info.PlayTimeSeconds)}",
                Modulate = new Color(0.75f, 0.75f, 0.75f),
            };

            var dateLabel = new Label
            {
                Text     = $"Saved: {info.Timestamp}",
                Modulate = new Color(0.55f, 0.55f, 0.55f),
            };

            infoCol.AddChild(nameLabel);
            infoCol.AddChild(timeLabel);
            infoCol.AddChild(dateLabel);
        }
        else
        {
            var emptyLabel = new Label
            {
                Text              = $"Slot {slot} — (Empty)",
                VerticalAlignment = VerticalAlignment.Center,
                Modulate          = new Color(0.5f, 0.5f, 0.5f),
            };
            emptyLabel.AddThemeFontSizeOverride("font_size", 14);
            infoCol.AddChild(emptyLabel);
        }

        // Button column
        var btnCol = new VBoxContainer();
        btnCol.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        btnCol.AddThemeConstantOverride("separation", 6);
        hbox.AddChild(btnCol);

        int capturedSlot = slot;

        if (PendingMode == MenuMode.Continue)
        {
            var loadBtn = new Button
            {
                Text              = "Load",
                Disabled          = info == null,
                CustomMinimumSize = new Vector2(100, 0),
            };
            loadBtn.Pressed += () => OnLoadPressed(capturedSlot);
            loadBtn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
            btnCol.AddChild(loadBtn);

            // Allow starting fresh from any slot even on the Continue screen
            var newBtn = new Button
            {
                Text              = "New Game",
                CustomMinimumSize = new Vector2(100, 0),
            };
            newBtn.Pressed += () =>
            {
                if (info != null) ShowOverwriteConfirm(capturedSlot);
                else OnNewGameInSlotPressed(capturedSlot);
            };
            newBtn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
            btnCol.AddChild(newBtn);
        }
        else // NewGame mode
        {
            var newBtn = new Button
            {
                Text              = info != null ? "Overwrite" : "New Game",
                CustomMinimumSize = new Vector2(100, 0),
            };
            newBtn.Pressed += () =>
            {
                if (info != null) ShowOverwriteConfirm(capturedSlot);
                else OnNewGameInSlotPressed(capturedSlot);
            };
            newBtn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
            btnCol.AddChild(newBtn);
        }

        parent.AddChild(card);
    }

    // ── Overwrite confirmation ────────────────────────────────────────────────

    private void ShowOverwriteConfirm(int slot)
    {
        if (_confirmOverlay != null) return;
        _pendingSlot = slot;

        AudioManager.Instance?.PlaySfx(UiSfx.Cursor);

        // Must use a CanvasLayer above the slot cards (layer 5) so the overlay renders on top
        _confirmOverlay = new CanvasLayer { Layer = 6 };
        AddChild(_confirmOverlay);

        var dimBg = new ColorRect
        {
            Color        = new Color(0f, 0f, 0f, 0.7f),
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        _confirmOverlay.AddChild(dimBg);

        var centerer = new CenterContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        _confirmOverlay.AddChild(centerer);

        var panel = new PanelContainer();
        UiTheme.ApplyPanelTheme(panel);
        centerer.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   20);
        margin.AddThemeConstantOverride("margin_right",  20);
        margin.AddThemeConstantOverride("margin_top",    16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        var label = new Label
        {
            Text                = $"Overwrite Slot {slot}? This cannot be undone.",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(label);

        var btnRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        btnRow.AddThemeConstantOverride("separation", 16);
        vbox.AddChild(btnRow);

        var yesBtn = new Button { Text = "Overwrite", CustomMinimumSize = new Vector2(110, 0) };
        yesBtn.Pressed += () =>
        {
            DismissConfirm();
            OnNewGameInSlotPressed(_pendingSlot);
        };
        yesBtn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
        btnRow.AddChild(yesBtn);

        var noBtn = new Button { Text = "Cancel", CustomMinimumSize = new Vector2(110, 0) };
        noBtn.Pressed += () =>
        {
            AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
            DismissConfirm();
        };
        noBtn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
        btnRow.AddChild(noBtn);

        UiTheme.ApplyToAllButtons(_confirmOverlay);
        UiTheme.ApplyPixelFontToAll(_confirmOverlay);

        noBtn.CallDeferred(Button.MethodName.GrabFocus);
    }

    private void DismissConfirm()
    {
        _confirmOverlay?.QueueFree();
        _confirmOverlay = null;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void OnLoadPressed(int slot)
    {
        if (_transitioning) return;
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        SaveManager.Instance.CurrentSlot = slot;
        var data = SaveManager.Instance.LoadGame();
        if (data == null) return;
        _transitioning = true;
        SaveManager.Instance.ApplyLoadedData(data);
        string map = string.IsNullOrEmpty(data.LastMapPath)
            ? "res://scenes/overworld/MAPP.tscn"
            : data.LastMapPath;
        _ = SceneTransition.Instance.GoToAsync(map);
    }

    private void OnNewGameInSlotPressed(int slot)
    {
        if (_transitioning) return;
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        _transitioning = true;
        SaveManager.Instance.CurrentSlot = slot;
        GameManager.Instance.ResetForNewGame();
        string nextScene = GameManager.Instance.GetFlag(Flags.IntroCutsceneSeen)
            ? "res://scenes/menus/CharacterCustomization.tscn"
            : "res://scenes/cutscenes/IntroCutscene.tscn";
        _ = SceneTransition.Instance.GoToAsync(nextScene);
    }

    private void OnBackPressed()
    {
        if (_transitioning) return;
        AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
        _transitioning = true;
        _ = SceneTransition.Instance.GoToAsync("res://scenes/menus/MainMenu.tscn");
    }
}
