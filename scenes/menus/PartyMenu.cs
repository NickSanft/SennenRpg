using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using System.Collections.Generic;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Party management menu with two sections: ACTIVE (up to 4, used in battle/overworld)
/// and RESERVE (benched members). Cross-section confirm swaps a member between active
/// and reserve. Within-section confirm swaps reorder active members.
///
/// Code-built UI following the StatsMenu / ClassChangeMenu pattern. CanvasLayer 51.
/// </summary>
public partial class PartyMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static Color Gold        => UiTheme.Gold;
    private static Color SubtleGrey  => UiTheme.SubtleGrey;
    private static Color ActiveGreen => UiTheme.HaveGreen;
    private static Color ReserveColor => new(0.6f, 0.5f, 0.7f);

    private VBoxContainer  _outer  = null!;
    private VBoxContainer  _activeRows = null!;
    private VBoxContainer  _reserveRows = null!;
    private Label          _hintLabel = null!;
    private readonly List<Button> _allButtons = new(); // active buttons first, then reserve
    private int _focusedIndex; // index into _allButtons
    private int _swapAnchor = -1;

    public override void _Ready()
    {
        Layer   = 51;
        Visible = false;
    }

    public void Open()
    {
        _swapAnchor = -1;
        BuildUI();
        UiTheme.ApplyPixelFontToAll(this);
        UiTheme.ApplyToAllButtons(this);
        Visible = true;
        TutorialManager.Instance?.Trigger(TutorialIds.PartyMenu);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;
        if (e.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            Close();
            return;
        }
        if (e.IsActionPressed("ui_left"))
        {
            ToggleRow(_focusedIndex);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (e.IsActionPressed("ui_right"))
        {
            PromoteToLeader(_focusedIndex);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (e.IsActionPressed("interact") || e.IsActionPressed("ui_accept"))
        {
            HandleSwapPress(_focusedIndex);
            GetViewport().SetInputAsHandled();
        }
    }

    private void Close()
    {
        AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    // ── UI construction ──────────────────────────────────────────────

    private void BuildUI()
    {
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();
        _allButtons.Clear();

        var overlay = new ColorRect
        {
            Color        = new Color(0f, 0f, 0f, 0.75f),
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(overlay);

        var centerer = new CenterContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(centerer);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(560f, 0f),
        };
        UiTheme.ApplyPanelTheme(panel);
        centerer.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   16);
        margin.AddThemeConstantOverride("margin_right",  16);
        margin.AddThemeConstantOverride("margin_top",    12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        _outer = new VBoxContainer();
        _outer.AddThemeConstantOverride("separation", 6);
        margin.AddChild(_outer);

        var title = new Label
        {
            Text                = "PARTY",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        _outer.AddChild(title);

        _outer.AddChild(new HSeparator());

        // Active section
        var activeHeader = new Label
        {
            Text                = "ACTIVE",
            HorizontalAlignment = HorizontalAlignment.Left,
            Modulate            = ActiveGreen,
        };
        activeHeader.AddThemeFontSizeOverride("font_size", 13);
        _outer.AddChild(activeHeader);

        _activeRows = new VBoxContainer();
        _activeRows.AddThemeConstantOverride("separation", 4);
        _outer.AddChild(_activeRows);

        // Reserve section
        _outer.AddChild(new HSeparator());
        var reserveHeader = new Label
        {
            Text                = "RESERVE",
            HorizontalAlignment = HorizontalAlignment.Left,
            Modulate            = ReserveColor,
        };
        reserveHeader.AddThemeFontSizeOverride("font_size", 13);
        _outer.AddChild(reserveHeader);

        _reserveRows = new VBoxContainer();
        _reserveRows.AddThemeConstantOverride("separation", 4);
        _outer.AddChild(_reserveRows);

        BuildMemberRows();

        _outer.AddChild(new HSeparator());

        _hintLabel = new Label
        {
            Text                = HintText(),
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode        = TextServer.AutowrapMode.WordSmart,
            Modulate            = SubtleGrey,
        };
        _hintLabel.AddThemeFontSizeOverride("font_size", 12);
        _outer.AddChild(_hintLabel);

        if (_allButtons.Count > 0)
            _allButtons[0].CallDeferred(Control.MethodName.GrabFocus);
    }

    private void BuildMemberRows()
    {
        foreach (var child in _activeRows.GetChildren())
            if (child is Node n) n.QueueFree();
        foreach (var child in _reserveRows.GetChildren())
            if (child is Node n) n.QueueFree();
        _allButtons.Clear();

        var party = GameManager.Instance.Party;

        // Active members
        for (int i = 0; i < party.Members.Count; i++)
        {
            var btn = MakeMemberButton(party.Members[i], i, isActive: true, party.LeaderIndex);
            _activeRows.AddChild(btn);
            _allButtons.Add(btn);
        }

        if (party.Members.Count == 0)
        {
            var empty = new Label
            {
                Text                = "No active members.",
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate            = SubtleGrey,
            };
            empty.AddThemeFontSizeOverride("font_size", 12);
            _activeRows.AddChild(empty);
        }

        // Reserve members
        for (int i = 0; i < party.ReserveMembers.Count; i++)
        {
            var btn = MakeMemberButton(party.ReserveMembers[i], i, isActive: false, -1);
            _reserveRows.AddChild(btn);
            _allButtons.Add(btn);
        }

        if (party.ReserveMembers.Count == 0)
        {
            var empty = new Label
            {
                Text                = "Empty.",
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate            = SubtleGrey,
            };
            empty.AddThemeFontSizeOverride("font_size", 12);
            _reserveRows.AddChild(empty);
        }
    }

    private Button MakeMemberButton(PartyMember member, int sectionIdx, bool isActive, int leaderIdx)
    {
        bool isLeader = isActive && sectionIdx == leaderIdx;
        int globalIdx = isActive ? sectionIdx : GameManager.Instance.Party.ActiveCount + sectionIdx;
        bool isAnchor = globalIdx == _swapAnchor;

        string leaderMark = isLeader ? "★" : " ";
        string rowLabel   = member.Row == FormationRow.Front ? "FRONT" : "BACK ";
        string anchorMark = isAnchor ? " «" : "";
        string section    = isActive ? "" : " [RSV]";

        string text =
            $"{leaderMark} {member.DisplayName,-10}  {member.Class,-10}  Lv{member.Level,2}  " +
            $"HP {member.CurrentHp,3}/{member.MaxHp,-3}  MP {member.CurrentMp,2}/{member.MaxMp,-2}  [{rowLabel}]{anchorMark}{section}";

        var btn = new Button
        {
            Text              = text,
            CustomMinimumSize = new Vector2(0f, 30f),
        };
        btn.AddThemeFontSizeOverride("font_size", 11);

        if (isLeader) btn.Modulate = ActiveGreen;
        else if (!isActive) btn.Modulate = ReserveColor;

        int capturedGlobal = globalIdx;
        btn.FocusEntered += () =>
        {
            _focusedIndex = capturedGlobal;
            AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
            // Resolve to memberId for stats/equipment menu selection
            var m = GetMemberAtGlobal(capturedGlobal);
            if (m != null) GameManager.Instance.SelectedMemberId = m.MemberId;
            _hintLabel.Text = HintText();
        };

        return btn;
    }

    // ── Actions ──────────────────────────────────────────────────────

    private void HandleSwapPress(int globalIdx)
    {
        var party = GameManager.Instance.Party;
        if (GetMemberAtGlobal(globalIdx) == null) return;

        if (_swapAnchor == -1)
        {
            _swapAnchor = globalIdx;
            AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
            RefreshRows();
            return;
        }
        if (_swapAnchor == globalIdx)
        {
            _swapAnchor = -1;
            AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
            RefreshRows();
            return;
        }

        bool anchorIsActive = _swapAnchor < party.ActiveCount;
        bool targetIsActive = globalIdx < party.ActiveCount;

        if (anchorIsActive && targetIsActive)
        {
            // Both active — reorder swap
            GameManager.Instance.SwapPartyMembers(_swapAnchor, globalIdx);
        }
        else if (anchorIsActive != targetIsActive)
        {
            // Cross-section swap
            int activeIdx  = anchorIsActive ? _swapAnchor : globalIdx;
            int reserveIdx = anchorIsActive
                ? globalIdx - party.ActiveCount
                : _swapAnchor - party.ActiveCount;

            if (!GameManager.Instance.SwapActiveReserve(activeIdx, reserveIdx))
            {
                AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
                _swapAnchor = -1;
                RefreshRows();
                return;
            }
        }
        // Reserve-reserve swap: no-op (not useful)

        _swapAnchor = -1;
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        RefreshRows();
    }

    private void PromoteToLeader(int globalIdx)
    {
        var party = GameManager.Instance.Party;
        if (globalIdx < 0 || globalIdx >= party.ActiveCount) return; // only active can be leader
        if (globalIdx == party.LeaderIndex) return;
        GameManager.Instance.SetPartyLeader(party.Members[globalIdx].MemberId);
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        RefreshRows();
    }

    private void ToggleRow(int globalIdx)
    {
        var member = GetMemberAtGlobal(globalIdx);
        if (member == null) return;
        member.Row = member.Row == FormationRow.Front ? FormationRow.Back : FormationRow.Front;
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        RefreshRows();
    }

    private void RefreshRows()
    {
        BuildMemberRows();
        UiTheme.ApplyPixelFontToAll(_activeRows);
        UiTheme.ApplyToAllButtons(_activeRows);
        UiTheme.ApplyPixelFontToAll(_reserveRows);
        UiTheme.ApplyToAllButtons(_reserveRows);
        _hintLabel.Text = HintText();
        int idx = System.Math.Clamp(_focusedIndex, 0, _allButtons.Count - 1);
        if (idx >= 0 && idx < _allButtons.Count)
            _allButtons[idx].CallDeferred(Control.MethodName.GrabFocus);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static PartyMember? GetMemberAtGlobal(int globalIdx)
    {
        var party = GameManager.Instance.Party;
        if (globalIdx < 0) return null;
        if (globalIdx < party.ActiveCount)
            return party.Members[globalIdx];
        int reserveIdx = globalIdx - party.ActiveCount;
        if (reserveIdx < party.ReserveMembers.Count)
            return party.ReserveMembers[reserveIdx];
        return null;
    }

    private string HintText()
    {
        if (_swapAnchor != -1)
            return "Pick a row to swap with...   [Confirm again] Cancel   [Esc] Back";

        var party = GameManager.Instance.Party;
        bool onActive = _focusedIndex < party.ActiveCount;
        return onActive
            ? "[←] Front/Back   [→] Set Leader   [Confirm] Swap   [Esc] Back"
            : "[←] Front/Back   [Confirm] Swap to Active   [Esc] Back";
    }
}
