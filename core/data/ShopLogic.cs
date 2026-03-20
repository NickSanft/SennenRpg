namespace SennenRpg.Core.Data;

/// <summary>
/// Pure shop transaction logic — no Godot runtime dependency.
/// All methods are static and side-effect-free so they can be unit-tested with NUnit.
/// </summary>
public static class ShopLogic
{
	/// <summary>Returns true if the player has enough gold to buy at the given price.</summary>
	public static bool CanAfford(int gold, int price) => price >= 0 && gold >= price;

	/// <summary>Returns the player's gold remaining after a successful purchase.</summary>
	public static int GoldAfterPurchase(int gold, int price) => gold - price;
}
