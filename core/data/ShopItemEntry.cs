using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// One entry in a vendor's shop — the item for sale and its gold price.
/// Assign these as sub-resources on a VendorNpc in the Godot inspector.
/// </summary>
[GlobalClass]
public partial class ShopItemEntry : Resource
{
	/// <summary>res:// path to the ItemData .tres file being sold.</summary>
	[Export] public string ItemDataPath { get; set; } = "";

	/// <summary>Gold cost to purchase this item.</summary>
	[Export] public int Price { get; set; } = 0;
}
