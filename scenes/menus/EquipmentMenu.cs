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
    private Control _panel       = null!;
    private Label   _statsLabel  = null!;

    // Item picker (visible when choosing what to equip / unequip)
    private Control        _itemPickerPanel = null!;
    private Label          _itemPickerTitle = null!;
    private VBoxContainer  _itemPickerList  = null!;

    private readonly Dictionary<EquipmentSlot, EquipmentSlotButton> _slotButtons = new();
    private EquipmentSlot? _activeSlot;

    // ── Setup ─────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        Layer   = 51;
        Visible = false;
        BuildUI();
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

        // Centred panel
        _panel = new Control();
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.CustomMinimumSize = new Vector2(460f, 0f);
        AddChild(_panel);

        var bg = new ColorRect
        {
            Color        = BgColour,
            AnchorRight  = 1f,
            AnchorBottom = 1f,
        };
        _panel.AddChild(bg);

        // Outer VBox
        var outer = new VBoxContainer
        {
            AnchorRight  = 1f,
            AnchorBottom = 1f,
            OffsetLeft   = 16f, OffsetRight  = -16f,
            OffsetTop    = 12f, OffsetBottom = -12f,
        };
        _panel.AddChild(outer);

        // Title
        var title = new Label
        {
            Text                = "★  EQUIPMENT  ★",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate            = Gold,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        outer.AddChild(title);
        outer.AddChild(new HSeparator());

        // Three-column content row
        var columns = new HBoxContainer();
        outer.AddChild(columns);

        // Left column
        var leftCol = new VBoxContainer { CustomMinimumSize = new Vector2(112f, 0f) };
        BuildSlotColumn(leftCol, LeftSlots);
        columns.AddChild(leftCol);

        // Centre column — stat panel + item picker (mutually exclusive)
        var centreCol = new VBoxContainer { CustomMinimumSize = new Vector2(220f, 0f) };
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
        hint.AddThemeFontSizeOverride("font_size", 9);
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
            _slotButtons[slot] = btn;
        }
    }

    private void BuildStatPanel(VBoxContainer parent)
    {
        // Portrait placeholder
        var portrait = new ColorRect
        {
            Color             = new Color(0.18f, 0.18f, 0.28f),
            CustomMinimumSize = new Vector2(0f, 56f),
        };
        parent.AddChild(portrait);

        var portraitLabel = new Label
        {
            Text                = "(portrait)",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
            AnchorRight  = 1f, AnchorBottom = 1f,
            Modulate     = SubtleGrey,
        };
        portraitLabel.AddThemeFontSizeOverride("font_size", 8);
        portrait.AddChild(portraitLabel);

        // Stats text
        _statsLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.Off,
        };
        _statsLabel.AddThemeFontSizeOverride("font_size", 11);
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
        _itemPickerTitle.AddThemeFontSizeOverride("font_size", 11);
        _itemPickerPanel.AddChild(_itemPickerTitle);

        _itemPickerPanel.AddChild(new HSeparator());

        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0f, 120f) };
        _itemPickerPanel.AddChild(scroll);

        _itemPickerList = new VBoxContainer();
        scroll.AddChild(_itemPickerList);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open()
    {
        Visible = true;
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
        if (!e.IsActionPressed("ui_cancel")) return;
        GetViewport().SetInputAsHandled();
        if (_itemPickerPanel.Visible)
            CloseItemPicker();
        else
            Close();
    }

    // ── Slot pressed ──────────────────────────────────────────────────────────

    private void OnSlotPressed(EquipmentSlot slot)
    {
        _activeSlot = slot;
        bool isEquipped = GameManager.Instance.EquippedItemPaths.ContainsKey(slot)
                       && !string.IsNullOrEmpty(GameManager.Instance.EquippedItemPaths[slot]);

        _itemPickerTitle.Text = isEquipped ? $"Slot: {slot}" : $"Equip {slot}";

        // Clear previous list
        foreach (var child in _itemPickerList.GetChildren())
            child.QueueFree();

        if (isEquipped)
        {
            // Offer to unequip current item
            string path    = GameManager.Instance.EquippedItemPaths[slot];
            var    data    = ResourceLoader.Exists(path) ? GD.Load<EquipmentData>(path) : null;
            string name    = data?.DisplayName ?? "???";
            string bonusStr = FormatBonuses(data);

            AddPickerOption($"Unequip: {name}{bonusStr}", () =>
            {
                GameManager.Instance.Unequip(slot);
                CloseItemPicker();
            });
        }
        else
        {
            // List owned equipment compatible with this slot
            bool any = false;
            foreach (string path in GameManager.Instance.OwnedEquipmentPaths)
            {
                if (!ResourceLoader.Exists(path)) continue;
                var data = GD.Load<EquipmentData>(path);
                if (data.Slot != slot) continue;

                var captured = path;
                string bonusStr = FormatBonuses(data);
                AddPickerOption($"{data.DisplayName}{bonusStr}", () =>
                {
                    GameManager.Instance.Equip(slot, captured);
                    CloseItemPicker();
                });
                any = true;
            }

            if (!any)
            {
                var none = new Label
                {
                    Text     = "No compatible items",
                    Modulate = SubtleGrey,
                };
                none.AddThemeFontSizeOverride("font_size", 10);
                _itemPickerList.AddChild(none);
            }
        }

        // Cancel option
        AddPickerOption("Cancel", CloseItemPicker);

        // Show picker, hide stat panel
        _statsLabel.Visible      = false;
        _itemPickerPanel.Visible = true;

        // Focus first picker button
        if (_itemPickerList.GetChildCount() > 0 && _itemPickerList.GetChild(0) is Button first)
            first.CallDeferred(Button.MethodName.GrabFocus);
    }

    private void AddPickerOption(string text, System.Action action)
    {
        var btn = new Button { Text = text };
        btn.AddThemeFontSizeOverride("font_size", 10);
        btn.Pressed += action;
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
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    // ── Refresh helpers ───────────────────────────────────────────────────────

    private void RefreshAll()
    {
        foreach (var (slot, btn) in _slotButtons)
        {
            string? name = null;
            if (GameManager.Instance.EquippedItemPaths.TryGetValue(slot, out string? path)
                && !string.IsNullOrEmpty(path)
                && ResourceLoader.Exists(path))
            {
                name = GD.Load<EquipmentData>(path).DisplayName;
            }
            btn.Refresh(name);
        }
        RefreshStatsLabel();
    }

    private void RefreshStatsLabel()
    {
        if (_statsLabel == null) return; // not yet built

        var gm   = GameManager.Instance;
        var base_ = gm.PlayerStats;
        var eff  = gm.EffectiveStats;

        string BonusStr(int eff_, int base__) =>
            eff_ > base__ ? $" [color=green](+{eff_ - base__})[/color]" : "";

        _statsLabel.Text =
            $"{gm.PlayerName}  LV {gm.PlayerLevel}\n" +
            $"━━━━━━━━━━━━━━━━━\n" +
            $"HP:   {eff.MaxHp,4}{BonusStr(eff.MaxHp, base_.MaxHp)}\n" +
            $"ATK:  {eff.Attack,4}{BonusStr(eff.Attack, base_.Attack)}\n" +
            $"DEF:  {eff.Defense,4}{BonusStr(eff.Defense, base_.Defense)}\n" +
            $"SPD:  {eff.Speed,4}{BonusStr(eff.Speed, base_.Speed)}\n" +
            $"MAG:  {eff.Magic,4}{BonusStr(eff.Magic, base_.Magic)}\n" +
            $"RES:  {eff.Resistance,4}{BonusStr(eff.Resistance, base_.Resistance)}\n" +
            $"LCK:  {eff.Luck,4}{BonusStr(eff.Luck, base_.Luck)}";
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
        return parts.Count > 0 ? $"  [{string.Join(", ", parts)}]" : "";
    }
}
