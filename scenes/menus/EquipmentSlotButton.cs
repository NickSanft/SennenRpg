using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Menus;

/// <summary>
/// Reusable button cell for a single equipment slot.
/// Shows the slot name and either the equipped item name or "—" when empty.
/// </summary>
public partial class EquipmentSlotButton : Button
{
    public EquipmentSlot Slot { get; private set; }

    private static readonly Color ColourEquipped = Colors.White;
    private static readonly Color ColourEmpty    = new(0.5f, 0.5f, 0.5f);

    /// <summary>Configure the button for a given slot. Call before or after AddChild.</summary>
    public void Init(EquipmentSlot slot)
    {
        Slot              = slot;
        CustomMinimumSize = new Vector2(104, 52);
        Refresh(null);
    }

    /// <summary>Update the displayed item name. Pass null to show the "empty" state.</summary>
    public void Refresh(string? equippedItemName)
    {
        Text        = $"{Slot}\n{equippedItemName ?? "—"}";
        SelfModulate = equippedItemName != null ? ColourEquipped : ColourEmpty;
    }
}
