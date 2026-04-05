using Godot;
using System.Collections.Generic;
using System.Linq;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Player stats + XP progress screen. Layer 52 — opened from PauseMenu.
/// All child nodes built in code. Shows base / equip-bonus / total columns and an XP bar.
///
/// Uses CenterContainer so the panel reliably centres itself after layout runs,
/// avoiding the SetAnchorsAndOffsetsPreset(Center) + zero-height pitfall.
/// </summary>
public partial class StatsMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static Color Gold       => UiTheme.Gold;
    private static readonly Color BarFg      = new(0.25f, 0.75f, 1.0f);
    private static readonly Color BarBg      = new(0.15f, 0.15f, 0.25f);
    private static Color SubtleGrey => UiTheme.SubtleGrey;

    private Label     _headerLabel   = null!;
    private Label     _xpLabel       = null!;
    private ColorRect _xpBar         = null!;
    private Label     _statsLabel    = null!;
    private Label     _classLabel    = null!;
    private Label     _bonusLabel    = null!;

    // ── Setup ─────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        Layer   = 52;
        Visible = false;
        BuildUI();
        UiTheme.ApplyPixelFontToAll(this);
    }

    private void BuildUI()
    {
        // Full-screen dim overlay
        var overlay = new ColorRect
        {
            Color        = new Color(0f, 0f, 0f, 0.75f),
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(overlay);

        // CenterContainer fills the viewport and centres its single child automatically.
        // This avoids the zero-height issue that affects Control + SetAnchorsAndOffsetsPreset(Center).
        var centerer = new CenterContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(centerer);

        // PanelContainer auto-sizes to content and provides the dark background via StyleBox.
        var panelContainer = new PanelContainer
        {
            CustomMinimumSize = new Vector2(420f, 0f),
        };
        UiTheme.ApplyPanelTheme(panelContainer);
        centerer.AddChild(panelContainer);

        // MarginContainer provides padding inside the panel.
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   16);
        margin.AddThemeConstantOverride("margin_right",  16);
        margin.AddThemeConstantOverride("margin_top",    12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panelContainer.AddChild(margin);

        // VBoxContainer holds all content rows.
        var outer = new VBoxContainer();
        outer.AddThemeConstantOverride("separation", 6);
        margin.AddChild(outer);

        // Title
        var title = new Label
        {
            Text                = "★  STATS  ★",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        outer.AddChild(title);

        outer.AddChild(new HSeparator());

        // Player / level header
        _headerLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _headerLabel.AddThemeFontSizeOverride("font_size", 12);
        outer.AddChild(_headerLabel);

        // XP label
        _xpLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _xpLabel.AddThemeFontSizeOverride("font_size", 12);
        outer.AddChild(_xpLabel);

        // XP bar: background ColorRect containing the fill bar as a child
        var xpBarBg = new ColorRect
        {
            Color             = BarBg,
            CustomMinimumSize = new Vector2(0f, 10f),
        };
        outer.AddChild(xpBarBg);

        _xpBar = new ColorRect
        {
            Color         = BarFg,
            AnchorTop     = 0f,
            AnchorBottom  = 1f,
            AnchorLeft    = 0f,
            AnchorRight   = 0f,   // set to progress in Refresh()
        };
        xpBarBg.AddChild(_xpBar);

        outer.AddChild(new HSeparator());

        // Column headers
        outer.AddChild(MakeStatHeaderRow());

        // Stat rows (one Label; monospace-aligned via fixed-width columns)
        _statsLabel = new Label { AutowrapMode = TextServer.AutowrapMode.Off };
        _statsLabel.AddThemeFontSizeOverride("font_size", 12);
        outer.AddChild(_statsLabel);

        outer.AddChild(new HSeparator());

        // Class levels section
        _classLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _classLabel.AddThemeFontSizeOverride("font_size", 16);
        outer.AddChild(_classLabel);

        outer.AddChild(new HSeparator());

        // Cross-class bonuses section
        var bonusHeader = new Label
        {
            Text                = "Cross-Class Bonuses",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        bonusHeader.AddThemeFontSizeOverride("font_size", 12);
        outer.AddChild(bonusHeader);

        _bonusLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _bonusLabel.AddThemeFontSizeOverride("font_size", 16);
        outer.AddChild(_bonusLabel);

        outer.AddChild(new HSeparator());

        var hint = new Label
        {
            Text                = "[Esc] Close",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 18);
        outer.AddChild(hint);
    }

    private static HBoxContainer MakeStatHeaderRow()
    {
        var row = new HBoxContainer();
        AddHeaderCell(row, "STAT",  80f, HorizontalAlignment.Left);
        AddHeaderCell(row, "BASE",  60f, HorizontalAlignment.Right);
        AddHeaderCell(row, "EQUIP", 60f, HorizontalAlignment.Right);
        AddHeaderCell(row, "TOTAL", 60f, HorizontalAlignment.Right);
        return row;
    }

    private static void AddHeaderCell(HBoxContainer row, string text, float width, HorizontalAlignment align)
    {
        var lbl = new Label
        {
            Text                = text,
            HorizontalAlignment = align,
            CustomMinimumSize   = new Vector2(width, 0f),
            Modulate            = new Color(0.55f, 0.55f, 0.55f),
        };
        lbl.AddThemeFontSizeOverride("font_size", 16);
        row.AddChild(lbl);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        Refresh();
        Visible = true;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;
        if (!e.IsActionPressed("ui_cancel")) return;
        GetViewport().SetInputAsHandled();
        AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        var gm    = GameManager.Instance;
        var base_ = gm.PlayerStats;
        var eff   = gm.EffectiveStats;
        int lv    = gm.PlayerLevel;

        _headerLabel.Text = $"{gm.PlayerName}   ·   Level {lv}";

        // XP progress
        int   currentXp   = gm.Exp;
        bool  atMax       = lv >= LevelData.MaxLevel;
        int   prevThresh  = LevelData.ExpThreshold(lv);
        int   nextThresh  = atMax ? prevThresh : LevelData.ExpThreshold(lv + 1);
        int   xpIntoLevel = currentXp - prevThresh;
        int   xpNeeded    = nextThresh - prevThresh;
        int   xpToGo      = atMax ? 0 : nextThresh - currentXp;
        float progress    = atMax || xpNeeded <= 0 ? 1f
                          : Mathf.Clamp((float)xpIntoLevel / xpNeeded, 0f, 1f);

        _xpLabel.Text = atMax
            ? $"EXP: {currentXp}   (MAX LEVEL)"
            : $"EXP: {currentXp} / {nextThresh}   ({xpToGo} to next level)";

        _xpBar.AnchorRight = progress;

        // Stat rows
        _statsLabel.Text =
              StatRow("HP",  base_.MaxHp,      eff.MaxHp)
            + StatRow("ATK", base_.Attack,     eff.Attack)
            + StatRow("DEF", base_.Defense,    eff.Defense)
            + StatRow("SPD", base_.Speed,      eff.Speed)
            + StatRow("MAG", base_.Magic,      eff.Magic)
            + StatRow("RES", base_.Resistance, eff.Resistance)
            + StatRow("LCK", base_.Luck,       eff.Luck)
            + StatRow("MP",  base_.MaxMp,      eff.MaxMp);

        // Class levels
        RefreshClassInfo(gm);

        // Cross-class bonuses
        RefreshBonuses(gm);
    }

    private void RefreshClassInfo(GameManager gm)
    {
        var lines = new List<string>();
        lines.Add($"Class: {gm.ActiveClass}");
        foreach (var cls in System.Enum.GetValues<PlayerClass>())
        {
            if (gm.ClassEntries.TryGetValue(cls, out var entry))
            {
                string marker = cls == gm.ActiveClass ? " *" : "";
                lines.Add($"  {cls,-10} Lv {entry.Level}{marker}");
            }
        }
        _classLabel.Text = string.Join("\n", lines);
    }

    private void RefreshBonuses(GameManager gm)
    {
        var classLevels = gm.ClassEntries.ToDictionary(kv => kv.Key, kv => kv.Value.Level);
        var lines = new List<string>();

        foreach (var bonus in CrossClassBonusRegistry.All)
        {
            bool earned = classLevels.TryGetValue(bonus.SourceClass, out int lv)
                       && lv >= bonus.RequiredLevel;
            string mark = earned ? "[Y]" : "[ ]";
            lines.Add($"{mark} {bonus.Description}");
        }

        _bonusLabel.Text = lines.Count > 0 ? string.Join("\n", lines) : "None yet.";
    }

    private static string StatRow(string name, int baseVal, int effVal)
    {
        int    bonus    = effVal - baseVal;
        string bonusStr = bonus > 0 ? $"+{bonus}" : "—";
        return $"{name,-6}{baseVal,8}{bonusStr,8}{effVal,8}\n";
    }
}
