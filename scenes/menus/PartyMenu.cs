using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;
using System.Collections.Generic;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Party management menu opened from the pause menu.
/// Lists every member with their class / level / HP / MP, lets the player promote
/// a different leader, swap two members in marching order, and toggle each member's
/// formation row (Front / Back). Selecting a row sets <see cref="GameManager.SelectedMemberId"/>
/// so the Stats and Equipment menus can read which member to display when those go
/// party-aware in Phase 6.
///
/// Code-built UI following the StatsMenu / ClassChangeMenu pattern. CanvasLayer 51,
/// matching the existing menu layer convention.
/// </summary>
public partial class PartyMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private static Color Gold        => UiTheme.Gold;
    private static Color SubtleGrey  => UiTheme.SubtleGrey;
    private static Color ActiveGreen => UiTheme.HaveGreen;

    private VBoxContainer  _outer  = null!;
    private VBoxContainer  _rowList = null!;
    private Label          _hintLabel = null!;
    private readonly List<Button> _memberButtons = new();
    private int _focusedIndex;
    private int _swapAnchor = -1; // -1 = no anchor; otherwise index to swap with on next confirm

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
            // Toggle formation row of focused member
            ToggleRow(_focusedIndex);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (e.IsActionPressed("ui_right"))
        {
            // Promote focused member to leader
            PromoteToLeader(_focusedIndex);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (e.IsActionPressed("interact") || e.IsActionPressed("ui_accept"))
        {
            // Begin / commit a two-step swap
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

    private void BuildUI()
    {
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();
        _memberButtons.Clear();

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

        // Member rows
        _rowList = new VBoxContainer();
        _rowList.AddThemeConstantOverride("separation", 4);
        _outer.AddChild(_rowList);

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

        if (_memberButtons.Count > 0)
            _memberButtons[0].CallDeferred(Control.MethodName.GrabFocus);
    }

    private void BuildMemberRows()
    {
        foreach (var child in _rowList.GetChildren())
            if (child is Node n) n.QueueFree();
        _memberButtons.Clear();

        var party = GameManager.Instance.Party;
        for (int i = 0; i < party.Members.Count; i++)
        {
            var member = party.Members[i];
            bool isLeader = i == party.LeaderIndex;
            bool isAnchor = i == _swapAnchor;

            string leaderMark   = isLeader ? "★" : " ";
            string rowLabel     = member.Row == FormationRow.Front ? "FRONT" : "BACK ";
            string anchorMark   = isAnchor ? " «" : "";

            string text =
                $"{leaderMark} {member.DisplayName,-10}  {member.Class,-10}  Lv{member.Level,2}  " +
                $"HP {member.CurrentHp,3}/{member.MaxHp,-3}  MP {member.CurrentMp,2}/{member.MaxMp,-2}  [{rowLabel}]{anchorMark}";

            var btn = new Button
            {
                Text              = text,
                CustomMinimumSize = new Vector2(0f, 30f),
            };
            btn.AddThemeFontSizeOverride("font_size", 11);
            if (isLeader) btn.Modulate = ActiveGreen;

            int captured = i;
            btn.FocusEntered += () =>
            {
                _focusedIndex = captured;
                AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
                GameManager.Instance.SelectedMemberId = GameManager.Instance.Party.Members[captured].MemberId;
                _hintLabel.Text = HintText();
            };

            _rowList.AddChild(btn);
            _memberButtons.Add(btn);
        }

        if (party.Members.Count == 0)
        {
            var empty = new Label
            {
                Text                = "No party members.",
                HorizontalAlignment = HorizontalAlignment.Center,
                Modulate            = SubtleGrey,
            };
            empty.AddThemeFontSizeOverride("font_size", 12);
            _rowList.AddChild(empty);
        }
    }

    // ── Actions ───────────────────────────────────────────────────────

    private void HandleSwapPress(int idx)
    {
        var party = GameManager.Instance.Party;
        if (idx < 0 || idx >= party.Members.Count) return;

        if (_swapAnchor == -1)
        {
            // First press — anchor this row.
            _swapAnchor = idx;
            AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
            RefreshRows();
            return;
        }
        if (_swapAnchor == idx)
        {
            // Cancel anchor.
            _swapAnchor = -1;
            AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
            RefreshRows();
            return;
        }
        // Second press — perform the swap. Route through GameManager so the
        // PartyOrderChanged signal fires and the overworld scene refreshes followers.
        GameManager.Instance.SwapPartyMembers(_swapAnchor, idx);
        _swapAnchor = -1;
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        RefreshRows();
    }

    private void PromoteToLeader(int idx)
    {
        var party = GameManager.Instance.Party;
        if (idx < 0 || idx >= party.Members.Count) return;
        if (idx == party.LeaderIndex) return;
        // Route through GameManager so PartyOrderChanged fires and the overworld
        // scene refreshes the leader's sprite + the follower chain.
        GameManager.Instance.SetPartyLeader(party.Members[idx].MemberId);
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        RefreshRows();
    }

    private void ToggleRow(int idx)
    {
        var party = GameManager.Instance.Party;
        if (idx < 0 || idx >= party.Members.Count) return;
        var member = party.Members[idx];
        member.Row = member.Row == FormationRow.Front ? FormationRow.Back : FormationRow.Front;
        AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
        RefreshRows();
    }

    private void RefreshRows()
    {
        BuildMemberRows();
        UiTheme.ApplyPixelFontToAll(_rowList);
        UiTheme.ApplyToAllButtons(_rowList);
        _hintLabel.Text = HintText();
        // Restore focus to the row at _focusedIndex if possible.
        int idx = System.Math.Clamp(_focusedIndex, 0, _memberButtons.Count - 1);
        if (idx >= 0 && idx < _memberButtons.Count)
            _memberButtons[idx].CallDeferred(Control.MethodName.GrabFocus);
    }

    private string HintText()
    {
        return _swapAnchor == -1
            ? "[←] Toggle Front/Back   [→] Set Leader   [Confirm] Swap   [Esc] Back"
            : "Pick a row to swap with…   [Confirm again] Cancel   [Esc] Back";
    }
}
