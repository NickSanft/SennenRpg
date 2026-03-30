using Godot;

namespace SennenRpg.Core.Data;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string ItemId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public Texture2D? Icon { get; set; }
    [Export] public int HealAmount { get; set; } = 0;
    /// <summary>
    /// When > 0 this item is a Repel: using it in battle grants this many
    /// steps of encounter immunity on the world map.
    /// </summary>
    [Export] public int RepelSteps { get; set; } = 0;
}
