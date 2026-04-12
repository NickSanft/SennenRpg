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

    // Phase 6 — member cycler. Index into GameManager.Party.Members for the
    // currently inspected member. Use ←/→ to cycle.
    private int _currentMemberIndex;

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

        // Wrapped in a scroll container so the now-12 cross-class entries can't push
        // the rest of the menu off screen on shorter viewports.
        var bonusScroll = new ScrollContainer
        {
            CustomMinimumSize    = new Vector2(0f, 110f),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        outer.AddChild(bonusScroll);

        _bonusLabel = new Label
        {
            AutowrapMode        = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _bonusLabel.AddThemeFontSizeOverride("font_size", 11);
        bonusScroll.AddChild(_bonusLabel);

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
        // Honour the cursor set by PartyMenu (if any) so the same member appears here.
        var party = GameManager.Instance.Party;
        _currentMemberIndex = 0;
        var allMembers = party.AllMembers;
        for (int i = 0; i < allMembers.Count; i++)
            if (allMembers[i].MemberId == GameManager.Instance.SelectedMemberId)
            {
                _currentMemberIndex = i;
                break;
            }
        Refresh();
        Visible = true;
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;

        // Cycle members with ←/→ when the menu is open.
        var party = GameManager.Instance.Party;
        if (party.TotalCount > 1)
        {
            if (e.IsActionPressed("ui_left"))
            {
                _currentMemberIndex = (_currentMemberIndex - 1 + party.TotalCount) % party.TotalCount;
                GameManager.Instance.SelectedMemberId = party.AllMembers[_currentMemberIndex].MemberId;
                AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
                Refresh();
                GetViewport().SetInputAsHandled();
                return;
            }
            if (e.IsActionPressed("ui_right"))
            {
                _currentMemberIndex = (_currentMemberIndex + 1) % party.TotalCount;
                GameManager.Instance.SelectedMemberId = party.AllMembers[_currentMemberIndex].MemberId;
                AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
                Refresh();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

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
        var party = gm.Party;
        if (party.IsEmpty)
        {
            _headerLabel.Text = "(no party)";
            return;
        }
        if (_currentMemberIndex < 0 || _currentMemberIndex >= party.TotalCount)
            _currentMemberIndex = 0;

        var member  = party.AllMembers[_currentMemberIndex];
        bool isSen  = member.MemberId == "sen";
        bool canCycle = party.TotalCount > 1;
        string left  = canCycle ? "◀ " : "  ";
        string right = canCycle ? " ▶" : "  ";

        // Sen reads from the live combat domain so the existing equipment / cross-class
        // pipeline still drives effective stats. Lily/Rain are computed from their
        // PartyMember directly via PartyMemberStatsLogic.
        int lv;
        int currentXp;
        int baseHp, baseAtk, baseDef, baseSpd, baseMag, baseRes, baseLck, baseMp;
        int effHp, effAtk, effDef, effSpd, effMag, effRes, effLck, effMp;

        if (isSen)
        {
            var b = gm.PlayerStats;
            var e = gm.EffectiveStats;
            lv         = gm.PlayerLevel;
            currentXp  = gm.Exp;
            baseHp     = b.MaxHp;      effHp  = e.MaxHp;
            baseAtk    = b.Attack;     effAtk = e.Attack;
            baseDef    = b.Defense;    effDef = e.Defense;
            baseSpd    = b.Speed;      effSpd = e.Speed;
            baseMag    = b.Magic;      effMag = e.Magic;
            baseRes    = b.Resistance; effRes = e.Resistance;
            baseLck    = b.Luck;       effLck = e.Luck;
            baseMp     = b.MaxMp;      effMp  = e.MaxMp;
        }
        else
        {
            lv        = member.Level;
            currentXp = member.Exp;

            var bonuses = SumBonusesForMember(member);
            var eff     = PartyMemberStatsLogic.ComputeEffective(member, bonuses);

            baseHp = member.MaxHp;      effHp  = eff.MaxHp;
            baseAtk = member.Attack;    effAtk = eff.Attack;
            baseDef = member.Defense;   effDef = eff.Defense;
            baseSpd = member.Speed;     effSpd = eff.Speed;
            baseMag = member.Magic;     effMag = eff.Magic;
            baseRes = member.Resistance; effRes = eff.Resistance;
            baseLck = member.Luck;      effLck = eff.Luck;
            baseMp  = member.MaxMp;     effMp  = eff.MaxMp;
        }

        _headerLabel.Text = $"{left}{member.DisplayName}{right}   ·   {member.Class}   ·   Level {lv}";

        // XP progress (uses LevelData thresholds the same way as Sen).
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
              StatRow("HP",  baseHp,  effHp)
            + StatRow("ATK", baseAtk, effAtk)
            + StatRow("DEF", baseDef, effDef)
            + StatRow("SPD", baseSpd, effSpd)
            + StatRow("MAG", baseMag, effMag)
            + StatRow("RES", baseRes, effRes)
            + StatRow("LCK", baseLck, effLck)
            + StatRow("MP",  baseMp,  effMp);

        // Class levels (only meaningful for Sen — Lily/Rain are locked).
        if (isSen) RefreshClassInfo(gm);
        else        _classLabel.Text = $"Class: {member.Class}  (locked)";

        // Cross-class bonuses (only Sen accumulates them — Lily/Rain show none).
        if (isSen) RefreshBonuses(gm);
        else        _bonusLabel.Text = "—";
    }

    /// <summary>
    /// Sum equipment bonuses for a non-Sen party member by walking their
    /// per-member EquippedItemPaths dictionary and reading each .tres.
    /// </summary>
    private static EquipmentBonuses SumBonusesForMember(PartyMember member)
    {
        var list = new List<EquipmentBonuses>();
        foreach (var kv in member.EquippedItemPaths)
        {
            if (string.IsNullOrEmpty(kv.Value) || !ResourceLoader.Exists(kv.Value)) continue;
            var data = GD.Load<EquipmentData>(kv.Value);
            if (data == null) continue;
            list.Add(data.Bonuses);
        }
        return EquipmentLogic.SumBonuses(list);
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
