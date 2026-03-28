using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Player stats + XP progress screen. Layer 52 — opened from PauseMenu.
/// All child nodes built in code. Shows base / total columns and an XP bar.
/// </summary>
public partial class StatsMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static readonly Color Gold       = new(1.0f, 0.85f, 0.1f);
    private static readonly Color GreenBonus = new(0.3f, 1.0f, 0.4f);
    private static readonly Color BarFg      = new(0.25f, 0.75f, 1.0f);   // cyan-blue for XP
    private static readonly Color BarBg      = new(0.15f, 0.15f, 0.25f);
    private static readonly Color SubtleGrey = new(0.55f, 0.55f, 0.55f);
    private static readonly Color BgColour   = new(0.07f, 0.07f, 0.12f, 1f);

    private Label      _headerLabel  = null!;
    private Label      _xpLabel      = null!;
    private ColorRect  _xpBar        = null!;
    private ColorRect  _xpBarBg      = null!;
    private Label      _statsLabel   = null!;

    // ── Setup ─────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        Layer   = 52;
        Visible = false;
        BuildUI();
    }

    private void BuildUI()
    {
        var overlay = new ColorRect
        {
            Color        = new Color(0f, 0f, 0f, 0.75f),
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(overlay);

        var panel = new Control();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        panel.CustomMinimumSize = new Vector2(360f, 0f);
        AddChild(panel);

        var bg = new ColorRect
        {
            Color        = BgColour,
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        panel.AddChild(bg);

        var outer = new VBoxContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
            OffsetLeft   = 16f, OffsetRight  = -16f,
            OffsetTop    = 12f, OffsetBottom = -12f,
        };
        outer.AddThemeConstantOverride("separation", 6);
        panel.AddChild(outer);

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
        _headerLabel.AddThemeFontSizeOverride("font_size", 13);
        outer.AddChild(_headerLabel);

        // XP label
        _xpLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _xpLabel.AddThemeFontSizeOverride("font_size", 11);
        outer.AddChild(_xpLabel);

        // XP bar (background + fill)
        _xpBarBg = new ColorRect
        {
            Color             = BarBg,
            CustomMinimumSize = new Vector2(0f, 10f),
        };
        outer.AddChild(_xpBarBg);

        _xpBar = new ColorRect
        {
            Color             = BarFg,
            AnchorBottom      = 1f,  // fills height of parent
            AnchorRight       = 0f,  // width controlled by OffsetRight
        };
        _xpBarBg.AddChild(_xpBar);

        outer.AddChild(new HSeparator());

        // Stats table
        var colHeader = new HBoxContainer();
        outer.AddChild(colHeader);

        var hStatCol = new Label { Text = "STAT", CustomMinimumSize = new Vector2(80f, 0f) };
        hStatCol.AddThemeFontSizeOverride("font_size", 10);
        hStatCol.Modulate = SubtleGrey;
        var hBase = new Label
        {
            Text                = "BASE",
            HorizontalAlignment = HorizontalAlignment.Right,
            CustomMinimumSize   = new Vector2(60f, 0f),
        };
        hBase.AddThemeFontSizeOverride("font_size", 10);
        hBase.Modulate = SubtleGrey;
        var hBonus = new Label
        {
            Text                = "EQUIP",
            HorizontalAlignment = HorizontalAlignment.Right,
            CustomMinimumSize   = new Vector2(60f, 0f),
        };
        hBonus.AddThemeFontSizeOverride("font_size", 10);
        hBonus.Modulate = SubtleGrey;
        var hTotal = new Label
        {
            Text                = "TOTAL",
            HorizontalAlignment = HorizontalAlignment.Right,
            CustomMinimumSize   = new Vector2(60f, 0f),
        };
        hTotal.AddThemeFontSizeOverride("font_size", 10);
        hTotal.Modulate = SubtleGrey;
        colHeader.AddChild(hStatCol);
        colHeader.AddChild(hBase);
        colHeader.AddChild(hBonus);
        colHeader.AddChild(hTotal);

        _statsLabel = new Label { AutowrapMode = TextServer.AutowrapMode.Off };
        _statsLabel.AddThemeFontSizeOverride("font_size", 11);
        outer.AddChild(_statsLabel);

        outer.AddChild(new HSeparator());

        var hint = new Label
        {
            Text                = "[Esc] Close",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 9);
        outer.AddChild(hint);
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
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void Refresh()
    {
        var gm   = GameManager.Instance;
        var base_ = gm.PlayerStats;
        var eff  = gm.EffectiveStats;
        int lv   = gm.PlayerLevel;

        // Header
        _headerLabel.Text = $"{gm.PlayerName}   Level {lv}";

        // XP
        int currentXp   = gm.Exp;
        bool atMax       = lv >= LevelData.MaxLevel;
        int  prevThresh  = LevelData.ExpThreshold(lv);
        int  nextThresh  = atMax ? prevThresh : LevelData.ExpThreshold(lv + 1);
        int  xpIntoLevel = currentXp - prevThresh;
        int  xpNeeded    = nextThresh - prevThresh;
        int  xpToGo      = atMax ? 0 : nextThresh - currentXp;
        float progress   = atMax || xpNeeded <= 0 ? 1f : (float)xpIntoLevel / xpNeeded;
        progress = Mathf.Clamp(progress, 0f, 1f);

        _xpLabel.Text = atMax
            ? $"EXP: {currentXp}  (MAX LEVEL)"
            : $"EXP: {currentXp} / {nextThresh}   ({xpToGo} to next level)";

        // XP bar fill (use OffsetRight to set proportional width via deferred call)
        _xpBar.CallDeferred(GodotObject.MethodName.Set, "anchor_right", (double)progress);

        // Stats table rows
        _statsLabel.Text = StatRow("HP",  base_.MaxHp,      eff.MaxHp)
                         + StatRow("ATK", base_.Attack,     eff.Attack)
                         + StatRow("DEF", base_.Defense,    eff.Defense)
                         + StatRow("SPD", base_.Speed,      eff.Speed)
                         + StatRow("MAG", base_.Magic,      eff.Magic)
                         + StatRow("RES", base_.Resistance, eff.Resistance)
                         + StatRow("LCK", base_.Luck,       eff.Luck)
                         + StatRow("MP",  base_.MaxMp,      eff.MaxMp);
    }

    private static string StatRow(string name, int baseVal, int effVal)
    {
        int bonus = effVal - baseVal;
        string bonusStr = bonus > 0 ? $"+{bonus}" : "—";
        return $"{name,-6}{baseVal,8}{bonusStr,8}{effVal,8}\n";
    }
}
