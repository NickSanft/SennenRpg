using Godot;

namespace SennenRpg.Core.Data;

public partial class ItemData : Resource
{
    [Export] public string ItemId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public Texture2D? Icon { get; set; }
    [Export] public int HealAmount { get; set; } = 0;
}
