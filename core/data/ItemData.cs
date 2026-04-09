using Godot;

namespace SennenRpg.Core.Data;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string ItemId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public Texture2D? Icon { get; set; }
    [Export] public ItemType Type { get; set; } = ItemType.Consumable;
    [Export] public int HealAmount { get; set; } = 0;
    /// <summary>
    /// When > 0 this item restores MP to a single party member on use. Items
    /// can carry both HealAmount and RestoreMp, but the standard pattern is to
    /// use one or the other (e.g. Bhata's Bugman's Ale is RestoreMp = 20).
    /// </summary>
    [Export] public int RestoreMp { get; set; } = 0;
    /// <summary>
    /// When > 0 this item is a Repel: using it in battle grants this many
    /// steps of encounter immunity on the world map.
    /// </summary>
    [Export] public int RepelSteps { get; set; } = 0;

    /// <summary>
    /// Gold value when selling this item. Used primarily by Junk items.
    /// </summary>
    [Export] public int SellValue { get; set; } = 0;
}
