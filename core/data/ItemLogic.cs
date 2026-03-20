namespace SennenRpg.Core.Data;

/// <summary>
/// Pure item-use calculations — no Godot runtime dependency.
/// All methods are static and side-effect-free so they can be unit-tested with NUnit.
/// </summary>
public static class ItemLogic
{
	/// <summary>
	/// Returns true when an item can currently be used: it must have a positive
	/// heal value and the player must not already be at maximum HP.
	/// </summary>
	public static bool CanUseItem(int healAmount, int currentHp, int maxHp)
		=> healAmount > 0 && currentHp < maxHp;

	/// <summary>Returns the player's HP after applying the heal, clamped to maxHp.</summary>
	public static int ApplyHeal(int healAmount, int currentHp, int maxHp)
		=> Math.Min(maxHp, currentHp + healAmount);

	/// <summary>
	/// Returns the HP actually restored (may be less than healAmount when the
	/// player is near max HP).
	/// </summary>
	public static int ActualHeal(int healAmount, int currentHp, int maxHp)
		=> ApplyHeal(healAmount, currentHp, maxHp) - currentHp;
}
