using Godot;
using System.Collections.Generic;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// OSRS-style worn equipment screen. Layer 51 — above PauseMenu (50), below SceneTransition (100).
///
/// Layout:
///   Left column  — Head / Body / Legs / Boots
///   Centre       — player portrait + effective stats panel  (or item-picker when active)
///   Right column — Weapon / Shield / Gloves / Accessory
///
/// All child nodes are built in code — no .tscn children needed.
/// </summary>
public partial class EquipmentMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly Color BgColour      = new(0.07f, 0.07f, 0.12f, 1f);
    private static readonly Color Gold          = new(1.0f, 0.85f, 0.1f);
    private static readonly Color GreenBonus    = new(0.3f, 1.0f, 0.4f);
    private static readonly Color SubtleGrey    = new(0.55f, 0.55f, 0.55f);

    // ── Slot column order ─────────────────────────────────────────────────────
    private static readonly EquipmentSlot[] LeftSlots  =
        { EquipmentSlot.Head, EquipmentSlot.Body, EquipmentSlot.Legs, EquipmentSlot.Boots };
    private static readonly EquipmentSlot[] RightSlots =
        { EquipmentSlot.Weapon, EquipmentSlot.Shield, EquipmentSlot.Gloves, EquipmentSlot.Accessory };

    // ── Node references ───────────────────────────────────────────────────────
    private RichTextLabel               _statsLabel    = null!;
    private SennenRpg.Scenes.Hud.AnimatedPortrait? _portrait;

    // Item picker (visible when choosing what to equip / unequip)
    private Control        _itemPickerPanel = null!;
    private Label          _itemPickerTitle = null!;
    private VBoxContainer  _itemPickerList  = null!;

    private readonly Dictionary<EquipmentSlot, EquipmentSlotButton> _slotButtons = new();
    private EquipmentSlot? _activeSlot;

    // Phase 6 — member cycler. Index into GameManager.Party.Members for the
    // currently inspected member. Use ←/→ to cycle when the menu is open.
    private int _currentMemberIndex;
    private Label _memberHeaderLabel = null!;

    // ── Setup ─────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        Layer   = 51;
        Visible = false;
        BuildUI();
        UiTheme.ApplyToAllButtons(this);
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

        // CenterContainer reliably centres the panel after layout runs
        var centerer = new CenterContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        AddChild(centerer);

        // PanelContainer auto-sizes to content; StyleBoxFlat provides the background + border
        var panelContainer = new PanelContainer
        {
            CustomMinimumSize = new Vector2(560f, 0f),
        };
        UiTheme.ApplyPanelTheme(panelContainer);
        centerer.AddChild(panelContainer);

        // MarginContainer for padding inside the panel
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   16);
        margin.AddThemeConstantOverride("margin_right",  16);
        margin.AddThemeConstantOverride("margin_top",    12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panelContainer.AddChild(margin);

        // Outer VBox — driven by MarginContainer
        var outer = new VBoxContainer();
        margin.AddChild(outer);

        // Title
        var title = new Label
        {
            Text                = "★  EQUIPMENT  ★",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        outer.AddChild(title);

        // Phase 6 — member cycler header (◀ Sen ▶ · Bard)
        _memberHeaderLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        _memberHeaderLabel.AddThemeFontSizeOverride("font_size", 12);
        outer.AddChild(_memberHeaderLabel);

        outer.AddChild(new HSeparator());

        // Three-column content row
        var columns = new HBoxContainer();
        outer.AddChild(columns);

        // Left column
        var leftCol = new VBoxContainer { CustomMinimumSize = new Vector2(112f, 0f) };
        BuildSlotColumn(leftCol, LeftSlots);
        columns.AddChild(leftCol);

        // Centre column — stat panel + item picker (mutually exclusive)
        var centreCol = new VBoxContainer { CustomMinimumSize = new Vector2(320f, 0f) };
        centreCol.AddThemeConstantOverride("separation", 4);
        columns.AddChild(centreCol);

        BuildStatPanel(centreCol);
        BuildItemPickerPanel(centreCol);

        // Right column
        var rightCol = new VBoxContainer { CustomMinimumSize = new Vector2(112f, 0f) };
        BuildSlotColumn(rightCol, RightSlots);
        columns.AddChild(rightCol);

        outer.AddChild(new HSeparator());

        // Hint
        var hint = new Label
        {
            Text                = "[Enter] Equip / Unequip   [Esc] Close",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = SubtleGrey,
        };
        hint.AddThemeFontSizeOverride("font_size", 18);
        outer.AddChild(hint);
    }

    private void BuildSlotColumn(VBoxContainer col, EquipmentSlot[] slots)
    {
        foreach (var slot in slots)
        {
            var btn = new EquipmentSlotButton();
            col.AddChild(btn);
            btn.Init(slot);
            var captured = slot;
            btn.Pressed += () => OnSlotPressed(captured);
            btn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
            _slotButtons[slot] = btn;
        }
    }

    private void BuildStatPanel(VBoxContainer parent)
    {
        // Member portrait — animated 2-frame loop using the member's overworld sheet.
        // The sprite is set in RefreshPortrait() each time the cycler moves.
        var portraitContainer = new ColorRect
        {
            Color             = new Color(0.18f, 0.18f, 0.28f),
            CustomMinimumSize = new Vector2(0f, 56f),
        };
        parent.AddChild(portraitContainer);

        _portrait = new SennenRpg.Scenes.Hud.AnimatedPortrait
        {
            PortraitSize         = new Vector2(48f, 48f),
            AnchorLeft           = 0.5f, AnchorRight = 0.5f,
            AnchorTop            = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft           = -24f, OffsetRight = 24f,
            OffsetTop            = -24f, OffsetBottom = 24f,
        };
        portraitContainer.AddChild(_portrait);

        // Stats text — RichTextLabel for BBCode colour support
        _statsLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent    = true,
            ScrollActive  = false,
        };
        _statsLabel.AddThemeFontSizeOverride("normal_font_size", 12);
        parent.AddChild(_statsLabel);

        RefreshStatsLabel();
    }

    private void BuildItemPickerPanel(VBoxContainer parent)
    {
        _itemPickerPanel = new VBoxContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(0f, 160f),
        };
        parent.AddChild(_itemPickerPanel);

        _itemPickerTitle = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        _itemPickerTitle.AddThemeFontSizeOverride("font_size", 12);
        _itemPickerPanel.AddChild(_itemPickerTitle);

        _itemPickerPanel.AddChild(new HSeparator());

        var scroll = new ScrollContainer
        {
            CustomMinimumSize    = new Vector2(0f, 120f),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal  = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
        };
        _itemPickerPanel.AddChild(scroll);

        _itemPickerList = new VBoxContainer();
        _itemPickerList.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
        scroll.AddChild(_itemPickerList);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        Visible = true;

        // Honour the cursor set by PartyMenu so the same member appears here.
        var party = GameManager.Instance.Party;
        _currentMemberIndex = 0;
        for (int i = 0; i < party.Members.Count; i++)
            if (party.Members[i].MemberId == GameManager.Instance.SelectedMemberId)
            {
                _currentMemberIndex = i;
                break;
            }

        RefreshAll();
        // Focus first left-column button
        if (_slotButtons.TryGetValue(EquipmentSlot.Head, out var first))
            first.CallDeferred(Button.MethodName.GrabFocus);
        GetViewport().GuiReleaseFocus();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible) return;

        // Cycle members with ←/→ when the picker is closed.
        if (!_itemPickerPanel.Visible)
        {
            var party = GameManager.Instance.Party;
            if (party.Count > 1)
            {
                if (e.IsActionPressed("ui_left"))
                {
                    _currentMemberIndex = (_currentMemberIndex - 1 + party.Count) % party.Count;
                    GameManager.Instance.SelectedMemberId = party.Members[_currentMemberIndex].MemberId;
                    AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
                    RefreshAll();
                    GetViewport().SetInputAsHandled();
                    return;
                }
                if (e.IsActionPressed("ui_right"))
                {
                    _currentMemberIndex = (_currentMemberIndex + 1) % party.Count;
                    GameManager.Instance.SelectedMemberId = party.Members[_currentMemberIndex].MemberId;
                    AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
                    RefreshAll();
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
        }

        if (!e.IsActionPressed("ui_cancel")) return;
        GetViewport().SetInputAsHandled();
        if (_itemPickerPanel.Visible)
            CloseItemPicker();
        else
            Close();
    }

    /// <summary>The party member currently shown in the menu, or null if the party is empty.</summary>
    private PartyMember? CurrentMember
    {
        get
        {
            var party = GameManager.Instance.Party;
            if (party.IsEmpty) return null;
            int idx = System.Math.Clamp(_currentMemberIndex, 0, party.Count - 1);
            return party.Members[idx];
        }
    }

    private bool IsCurrentSen => CurrentMember?.MemberId == "sen";

    // ── Slot pressed ──────────────────────────────────────────────────────────

    private void OnSlotPressed(EquipmentSlot slot)
    {
        _activeSlot = slot;
        var gm     = GameManager.Instance;
        var member = CurrentMember;
        if (member == null) return;
        bool isSen = IsCurrentSen;

        // For Sen, the InventoryData dicts are canonical (and include Lily-forged dynamic
        // equipment). For non-Sen, use the per-member dict on PartyMember (static items only).
        bool hasStaticEquipped;
        bool hasDynamicEquipped;

        if (isSen)
        {
            hasStaticEquipped = gm.EquippedItemPaths.ContainsKey(slot)
                              && !string.IsNullOrEmpty(gm.EquippedItemPaths[slot]);
            hasDynamicEquipped = gm.EquippedDynamicItemIds.ContainsKey(slot);
        }
        else
        {
            hasStaticEquipped = member.EquippedItemPaths.TryGetValue(slot.ToString(), out string? mp)
                              && !string.IsNullOrEmpty(mp);
            hasDynamicEquipped = false; // non-Sen don't use Lily forge slots in v1
        }
        bool isEquipped = hasStaticEquipped || hasDynamicEquipped;

        _itemPickerTitle.Text = $"{slot}";

        // Clear previous list
        foreach (var child in _itemPickerList.GetChildren())
            child.QueueFree();

        // Unequip option — always shown when something is equipped
        if (hasStaticEquipped)
        {
            string path = isSen ? gm.EquippedItemPaths[slot]
                                 : member.EquippedItemPaths[slot.ToString()];
            var    data     = ResourceLoader.Exists(path) ? GD.Load<EquipmentData>(path) : null;
            string bonusStr = FormatBonuses(data);
            AddPickerOption($"[Unequip] {data?.DisplayName ?? "???"}{bonusStr}", () =>
            {
                AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
                if (isSen)
                {
                    gm.Unequip(slot);
                }
                else
                {
                    // Mirror gm.Unequip's behaviour: return the item to the shared bag
                    // so it stops being "owned" by this member and becomes available
                    // again to other members and the inventory listing.
                    string returningPath = member.EquippedItemPaths[slot.ToString()];
                    member.EquippedItemPaths.Remove(slot.ToString());
                    if (!string.IsNullOrEmpty(returningPath))
                        gm.AddEquipment(returningPath);
                }
                CloseItemPicker();
            });
        }
        else if (hasDynamicEquipped && isSen)
        {
            string dynId    = gm.EquippedDynamicItemIds[slot];
            var    dynItem  = gm.DynamicEquipmentInventory.Find(e => e.Id == dynId);
            string bonusStr = FormatDynamicBonuses(dynItem);
            AddPickerOption($"[Unequip] {dynItem?.DisplayName ?? "???"}{bonusStr}", () =>
            {
                AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
                gm.UnequipDynamic(slot);
                CloseItemPicker();
            });
        }

        // Static .tres equipment available for this slot — shared bag for all members.
        bool any = false;
        foreach (string path in gm.OwnedEquipmentPaths)
        {
            if (!ResourceLoader.Exists(path)) continue;
            var data = GD.Load<EquipmentData>(path);
            if (data == null || data.Slot != slot) continue;

            var captured = path;
            string bonusStr = FormatBonuses(data);
            AddPickerOption($"{data.DisplayName}{bonusStr}", () =>
            {
                AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
                EquipFromBagOrTransfer(slot, captured, member, isSen, hasStaticEquipped, hasDynamicEquipped);
                CloseItemPicker();
            });
            any = true;
        }

        // Items currently equipped by OTHER members — show with owner suffix and
        // auto-transfer on pick. Without this, Sen takes everything during character
        // setup and the other recruits see "No compatible items" on every slot.
        foreach (var otherOwner in EnumerateItemOwnersForSlot(slot, excludeMember: member))
        {
            if (!ResourceLoader.Exists(otherOwner.Path)) continue;
            var data = GD.Load<EquipmentData>(otherOwner.Path);
            if (data == null) continue;

            var capturedPath  = otherOwner.Path;
            var capturedOwner = otherOwner.OwnerMember;
            string bonusStr = FormatBonuses(data);
            string ownerName = capturedOwner == null ? "Sen" : capturedOwner.DisplayName;
            AddPickerOption($"{data.DisplayName}{bonusStr}  ({ownerName})", () =>
            {
                AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
                // Step 1 — unequip from the current owner, returning to the bag.
                if (capturedOwner == null)
                {
                    gm.Unequip(slot); // Sen — also handles signal emission
                }
                else
                {
                    capturedOwner.EquippedItemPaths.Remove(slot.ToString());
                    gm.AddEquipment(capturedPath);
                }
                // Step 2 — equip on the active member.
                EquipFromBagOrTransfer(slot, capturedPath, member, isSen, hasStaticEquipped, hasDynamicEquipped);
                CloseItemPicker();
            });
            any = true;
        }

        // Lily-forged dynamic equipment available for this slot — Sen only.
        if (isSen)
        {
            foreach (var dynItem in gm.DynamicEquipmentInventory)
            {
                if (dynItem.Slot != slot) continue;
                if (gm.EquippedDynamicItemIds.ContainsValue(dynItem.Id)) continue;

                var captured = dynItem.Id;
                string bonusStr = FormatDynamicBonuses(dynItem);
                AddPickerOption($"{dynItem.DisplayName}{bonusStr}", () =>
                {
                    AudioManager.Instance?.PlaySfx(UiSfx.Confirm);
                    if (hasStaticEquipped) gm.Unequip(slot);
                    gm.EquipDynamic(slot, captured);
                    CloseItemPicker();
                });
                any = true;
            }
        }

        if (!isEquipped && !any)
        {
            var none = new Label
            {
                Text     = "No compatible items",
                Modulate = SubtleGrey,
            };
            none.AddThemeFontSizeOverride("font_size", 16);
            _itemPickerList.AddChild(none);
        }

        // Cancel option
        AddPickerOption("Cancel", CloseItemPicker);

        // Show picker, hide stat panel
        _statsLabel.Visible      = false;
        _itemPickerPanel.Visible = true;
        UiTheme.ApplyPixelFontToAll(_itemPickerPanel);
        UiTheme.ApplyToAllButtons(_itemPickerPanel);

        // Focus first picker button
        if (_itemPickerList.GetChildCount() > 0 && _itemPickerList.GetChild(0) is Button first)
            first.CallDeferred(Button.MethodName.GrabFocus);
    }

    /// <summary>
    /// Returns true when an item path is already equipped by a party member other than
    /// <paramref name="excludeMember"/>. Sen's equipment lives in <c>InventoryData.EquippedItemPaths</c>;
    /// other members store their slots on the PartyMember directly.
    /// </summary>
    private static bool IsItemEquippedByAnotherMember(string path, PartyMember excludeMember)
    {
        var gm = GameManager.Instance;

        // Sen
        if (excludeMember.MemberId != "sen")
        {
            foreach (var kv in gm.EquippedItemPaths)
                if (kv.Value == path) return true;
        }

        // Other party members
        foreach (var m in gm.Party.Members)
        {
            if (m.MemberId == excludeMember.MemberId) continue;
            if (m.MemberId == "sen") continue; // already handled above
            foreach (var kv in m.EquippedItemPaths)
                if (kv.Value == path) return true;
        }
        return false;
    }

    /// <summary>
    /// One entry in the "currently held by another member" list. <see cref="OwnerMember"/>
    /// is null when the current owner is Sen (whose equipment lives on GameManager
    /// rather than on a PartyMember).
    /// </summary>
    private readonly record struct ItemOwnership(string Path, PartyMember? OwnerMember);

    /// <summary>
    /// Walks every party member that isn't <paramref name="excludeMember"/> and yields
    /// the items they currently have equipped in <paramref name="slot"/>. Used by the
    /// item picker to make cross-member transfers possible.
    /// </summary>
    private static System.Collections.Generic.IEnumerable<ItemOwnership> EnumerateItemOwnersForSlot(
        EquipmentSlot slot, PartyMember excludeMember)
    {
        var gm = GameManager.Instance;

        // Sen — equipment dict lives on GameManager
        if (excludeMember.MemberId != "sen")
        {
            if (gm.EquippedItemPaths.TryGetValue(slot, out var senPath) && !string.IsNullOrEmpty(senPath))
                yield return new ItemOwnership(senPath, OwnerMember: null);
        }

        // Other recruits — each owns their own EquippedItemPaths dict
        foreach (var m in gm.Party.Members)
        {
            if (m.MemberId == excludeMember.MemberId) continue;
            if (m.MemberId == "sen") continue; // already handled above
            if (m.EquippedItemPaths.TryGetValue(slot.ToString(), out string? p) && !string.IsNullOrEmpty(p))
                yield return new ItemOwnership(p, OwnerMember: m);
        }
    }

    /// <summary>
    /// Apply an item from the shared bag to a slot on the active member. Mirrors
    /// the inline logic that used to live in OnSlotPressed but factored out so
    /// the cross-member transfer path can call it after unequipping the previous
    /// owner.
    /// </summary>
    private static void EquipFromBagOrTransfer(
        EquipmentSlot slot, string path, PartyMember member,
        bool isSen, bool hasStaticEquipped, bool hasDynamicEquipped)
    {
        var gm = GameManager.Instance;
        if (isSen)
        {
            if (hasDynamicEquipped) gm.UnequipDynamic(slot);
            gm.Equip(slot, path);
        }
        else
        {
            // Return any currently-equipped item to the shared bag.
            if (member.EquippedItemPaths.TryGetValue(slot.ToString(), out string? prev)
                && !string.IsNullOrEmpty(prev))
            {
                gm.AddEquipment(prev);
            }
            gm.OwnedEquipmentPaths.Remove(path);
            member.EquippedItemPaths[slot.ToString()] = path;
        }
    }

    private void AddPickerOption(string text, System.Action action)
    {
        var btn = new Button
        {
            Text                = text,
            SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
            ClipText            = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        btn.AddThemeFontSizeOverride("font_size", 9);
        btn.Pressed += action;
        btn.FocusEntered += () => AudioManager.Instance?.PlaySfx(UiSfx.Cursor);
        _itemPickerList.AddChild(btn);
    }

    private void CloseItemPicker()
    {
        _itemPickerPanel.Visible = false;
        _statsLabel.Visible      = true;
        RefreshAll();

        // Restore focus to the active slot button
        if (_activeSlot.HasValue && _slotButtons.TryGetValue(_activeSlot.Value, out var btn))
            btn.CallDeferred(Button.MethodName.GrabFocus);
        _activeSlot = null;
    }

    private void Close()
    {
        AudioManager.Instance?.PlaySfx(UiSfx.Cancel);
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    // ── Refresh helpers ───────────────────────────────────────────────────────

    private void RefreshAll()
    {
        var gm     = GameManager.Instance;
        var member = CurrentMember;
        bool isSen = IsCurrentSen;

        // Header — ◀ Name ▶ · Class
        if (member != null)
        {
            string left  = gm.Party.Count > 1 ? "◀ " : "  ";
            string right = gm.Party.Count > 1 ? " ▶" : "  ";
            _memberHeaderLabel.Text = $"{left}{member.DisplayName}{right}   ·   {member.Class}";
        }

        foreach (var (slot, btn) in _slotButtons)
        {
            string? name = null;

            if (isSen)
            {
                if (gm.EquippedItemPaths.TryGetValue(slot, out string? path)
                    && !string.IsNullOrEmpty(path)
                    && ResourceLoader.Exists(path))
                {
                    name = GD.Load<EquipmentData>(path).DisplayName;
                }
                else if (gm.EquippedDynamicItemIds.TryGetValue(slot, out string? dynId))
                {
                    name = gm.DynamicEquipmentInventory.Find(e => e.Id == dynId)?.DisplayName;
                }
            }
            else if (member != null)
            {
                if (member.EquippedItemPaths.TryGetValue(slot.ToString(), out string? path)
                    && !string.IsNullOrEmpty(path)
                    && ResourceLoader.Exists(path))
                {
                    name = GD.Load<EquipmentData>(path).DisplayName;
                }
            }

            btn.Refresh(name);
        }
        RefreshStatsLabel();
        RefreshPortrait();
        UiTheme.ApplyPixelFontToAll(this);
        UiTheme.ApplyToAllButtons(this);
    }

    /// <summary>
    /// Update the portrait TextureRect to show the current member's first overworld
    /// frame. Falls back to Sen's overworld sprite when the member doesn't have one
    /// configured.
    /// </summary>
    private void RefreshPortrait()
    {
        if (_portrait == null) return;

        var member = CurrentMember;
        string path = member?.OverworldSpritePath ?? "";
        if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path))
            path = "res://assets/sprites/player/Sen_Overworld.png";

        // Lily / Rain / Sen overworld sheets are all 32×16 (two 16×16 frames).
        // The animated portrait widget loops between both frames.
        _portrait.SetSpriteSheet(path);
    }

    private void RefreshStatsLabel()
    {
        if (_statsLabel == null) return; // not yet built

        var gm     = GameManager.Instance;
        var member = CurrentMember;
        if (member == null)
        {
            _statsLabel.Text = "(no party)";
            return;
        }

        string BonusStr(int eff_, int base__) =>
            eff_ > base__ ? $" [color=green](+{eff_ - base__})[/color]" : "";

        if (IsCurrentSen)
        {
            var base_ = gm.PlayerStats;
            var eff   = gm.EffectiveStats;
            _statsLabel.Text =
                $"{member.DisplayName}  LV {gm.PlayerLevel}\n" +
                $"━━━━━━━━━━━━━━━━━\n" +
                $"HP:   {eff.MaxHp,4}{BonusStr(eff.MaxHp, base_.MaxHp)}\n" +
                $"ATK:  {eff.Attack,4}{BonusStr(eff.Attack, base_.Attack)}\n" +
                $"DEF:  {eff.Defense,4}{BonusStr(eff.Defense, base_.Defense)}\n" +
                $"SPD:  {eff.Speed,4}{BonusStr(eff.Speed, base_.Speed)}\n" +
                $"MAG:  {eff.Magic,4}{BonusStr(eff.Magic, base_.Magic)}\n" +
                $"RES:  {eff.Resistance,4}{BonusStr(eff.Resistance, base_.Resistance)}\n" +
                $"LCK:  {eff.Luck,4}{BonusStr(eff.Luck, base_.Luck)}";
        }
        else
        {
            var bonuses = SumBonusesForMember(member);
            var eff     = PartyMemberStatsLogic.ComputeEffective(member, bonuses);
            _statsLabel.Text =
                $"{member.DisplayName}  LV {member.Level}\n" +
                $"━━━━━━━━━━━━━━━━━\n" +
                $"HP:   {eff.MaxHp,4}{BonusStr(eff.MaxHp, member.MaxHp)}\n" +
                $"ATK:  {eff.Attack,4}{BonusStr(eff.Attack, member.Attack)}\n" +
                $"DEF:  {eff.Defense,4}{BonusStr(eff.Defense, member.Defense)}\n" +
                $"SPD:  {eff.Speed,4}{BonusStr(eff.Speed, member.Speed)}\n" +
                $"MAG:  {eff.Magic,4}{BonusStr(eff.Magic, member.Magic)}\n" +
                $"RES:  {eff.Resistance,4}{BonusStr(eff.Resistance, member.Resistance)}\n" +
                $"LCK:  {eff.Luck,4}{BonusStr(eff.Luck, member.Luck)}";
        }
    }

    /// <summary>
    /// Sum equipment bonuses for a non-Sen party member by walking their per-member
    /// EquippedItemPaths dict and reading each .tres.
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

    private static string FormatBonuses(EquipmentData? data)
    {
        if (data == null) return "";
        var parts = new System.Collections.Generic.List<string>();
        if (data.BonusMaxHp      != 0) parts.Add($"HP+{data.BonusMaxHp}");
        if (data.BonusAttack     != 0) parts.Add($"ATK+{data.BonusAttack}");
        if (data.BonusDefense    != 0) parts.Add($"DEF+{data.BonusDefense}");
        if (data.BonusMagic      != 0) parts.Add($"MAG+{data.BonusMagic}");
        if (data.BonusResistance != 0) parts.Add($"RES+{data.BonusResistance}");
        if (data.BonusSpeed      != 0) parts.Add($"SPD+{data.BonusSpeed}");
        if (data.BonusLuck       != 0) parts.Add($"LCK+{data.BonusLuck}");
        if (parts.Count == 0) return "";

        // Class affinity hint based on the item's strongest stat bonus
        string affinity = GetClassAffinity(data);
        string affinityStr = !string.IsNullOrEmpty(affinity) ? $" ({affinity})" : "";
        return $"  [{string.Join(", ", parts)}]{affinityStr}";
    }

    private static string GetClassAffinity(EquipmentData data)
    {
        // Find the dominant bonus stat and map to the class that values it most
        int maxBonus = 0;
        string stat = "";
        if (data.BonusAttack     > maxBonus) { maxBonus = data.BonusAttack;     stat = "ATK"; }
        if (data.BonusDefense    > maxBonus) { maxBonus = data.BonusDefense;    stat = "DEF"; }
        if (data.BonusMagic      > maxBonus) { maxBonus = data.BonusMagic;      stat = "MAG"; }
        if (data.BonusSpeed      > maxBonus) { maxBonus = data.BonusSpeed;      stat = "SPD"; }
        if (data.BonusResistance > maxBonus) { maxBonus = data.BonusResistance; stat = "RES"; }
        if (data.BonusLuck       > maxBonus) { maxBonus = data.BonusLuck;       stat = "LCK"; }
        if (maxBonus == 0) return "";
        return stat switch
        {
            "ATK" or "DEF" => "Fighter",
            "MAG" or "RES" => "Mage",
            "SPD" or "LCK" => "Ranger",
            _ => "",
        };
    }

    private static string FormatDynamicBonuses(DynamicEquipmentSave? data)
    {
        if (data == null) return "";
        var parts = new System.Collections.Generic.List<string>();
        if (data.BonusMaxHp   != 0) parts.Add($"HP+{data.BonusMaxHp}");
        if (data.BonusAttack  != 0) parts.Add($"ATK+{data.BonusAttack}");
        if (data.BonusDefense != 0) parts.Add($"DEF+{data.BonusDefense}");
        if (data.BonusMagic   != 0) parts.Add($"MAG+{data.BonusMagic}");
        if (data.BonusSpeed   != 0) parts.Add($"SPD+{data.BonusSpeed}");
        if (data.BonusLuck    != 0) parts.Add($"LCK+{data.BonusLuck}");
        return parts.Count > 0 ? $"  [{string.Join(", ", parts)}]" : "";
    }
}
