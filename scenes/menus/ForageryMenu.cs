using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// "Foragery" — pause menu codex of every forageable item the player has discovered.
/// Locked entries render as silhouettes; unlocked entries show first-found timestamp,
/// total finds, current Perfect-streak, and personal-best grade.
/// Sorted alphabetically by display name.
/// </summary>
public partial class ForageryMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    /// <summary>
    /// Master list of every forageable item path. Locked entries appear here even if
    /// the player has never found them — they just render as silhouettes.
    /// </summary>
    private static readonly string[] AllForageablePaths =
    [
        "res://resources/items/junk_anima_slug_slime.tres",
        "res://resources/items/junk_flopsin_hairball.tres",
        "res://resources/items/junk_gravi_shard.tres",
        "res://resources/items/junk_astral_flower.tres",
    ];

    private static Color Gold       => UiTheme.Gold;
    private static Color HaveGreen  => UiTheme.HaveGreen;
    private static Color SubtleGrey => UiTheme.SubtleGrey;

    public override void _Ready()
    {
        Layer   = 51;
        Visible = false;
    }

    public void Open()
    {
        BuildUi();
        UiTheme.ApplyPixelFontToAll(this);
        UiTheme.ApplyToAllButtons(this);
        Visible = true;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;
        if (!e.IsActionPressed("ui_cancel")) return;
        GetViewport().SetInputAsHandled();
        Close();
    }

    private void Close()
    {
        AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    // ── UI ────────────────────────────────────────────────────────────

    private void BuildUi()
    {
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();

        var overlay = new ColorRect
        {
            Color = UiTheme.OverlayDim,
            AnchorRight = 1f, AnchorBottom = 1f,
        };
        AddChild(overlay);

        var centerer = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
        AddChild(centerer);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(560f, 0f) };
        UiTheme.ApplyPanelTheme(panel);
        centerer.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",  16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top",   12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        // Title
        var title = new Label
        {
            Text                = "FORAGERY",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(title);

        // Streak / total summary
        var gm    = GameManager.Instance;
        var codex = gm.ForageCodex;
        int total = ForageCodexLogic.TotalFinds(codex.Entries);
        int known = codex.Entries.Count;

        var summary = new Label
        {
            Text = $"Discovered: {known}/{AllForageablePaths.Length}    " +
                   $"Total finds: {total}    " +
                   $"Streak: {gm.ForageStreak}",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        summary.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(summary);
        vbox.AddChild(new HSeparator());

        // Build sorted entry list — every known forageable item, sorted alphabetically
        // by display name. Locked entries are still listed (silhouettes).
        var displayMap = LoadDisplayMap();
        string DisplayOf(string path) => displayMap.GetValueOrDefault(path, path.GetFile());

        var sortedAll = new List<string>(AllForageablePaths);
        sortedAll.Sort((a, b) => string.Compare(
            DisplayOf(a), DisplayOf(b), System.StringComparison.OrdinalIgnoreCase));

        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(list);

        foreach (var path in sortedAll)
            list.AddChild(BuildEntryRow(path, codex, displayMap));

        vbox.AddChild(new HSeparator());

        var hint = new Label
        {
            Text                = "[Esc] Back",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(hint);
    }

    private static Dictionary<string, string> LoadDisplayMap()
    {
        var map = new Dictionary<string, string>();
        foreach (var path in AllForageablePaths)
        {
            if (!ResourceLoader.Exists(path)) continue;
            var item = GD.Load<ItemData>(path);
            if (item != null && !string.IsNullOrEmpty(item.DisplayName))
                map[path] = item.DisplayName;
        }
        return map;
    }

    private static Control BuildEntryRow(
        string path,
        ForageCodexData codex,
        Dictionary<string, string> displayMap)
    {
        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 2);

        bool unlocked = codex.Entries.TryGetValue(path, out var entry);
        string displayName = unlocked
            ? displayMap.GetValueOrDefault(path, path.GetFile())
            : "???";

        var nameLabel = new Label { Text = displayName };
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.AddThemeColorOverride("font_color", unlocked ? Gold : SubtleGrey);
        row.AddChild(nameLabel);

        if (unlocked && entry != null)
        {
            string ts = entry.FirstFoundUtc == default
                ? ""
                : entry.FirstFoundUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            var grade = (ForageLogic.ForageGrade)entry.BestGradeRaw;

            var detail = new Label
            {
                Text     = $"First found: {ts}   x{entry.TimesFound}   Best: {ForageLogic.GradeLabel(grade)}",
                Modulate = HaveGreen,
            };
            detail.AddThemeFontSizeOverride("font_size", 11);
            row.AddChild(detail);
        }
        else
        {
            var detail = new Label
            {
                Text     = "Not yet discovered.",
                Modulate = SubtleGrey,
            };
            detail.AddThemeFontSizeOverride("font_size", 11);
            row.AddChild(detail);
        }

        return row;
    }
}
