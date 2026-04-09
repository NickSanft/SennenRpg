namespace SennenRpg.Core.Data;

/// <summary>
/// Pure item-use calculations — no Godot runtime dependency.
/// All methods are static and side-effect-free so they can be unit-tested with NUnit.
/// </summary>
public static class ItemLogic
{
	/// <summary>
	/// HP-only legacy form. Returns true when the item heals more than 0 and the
	/// target isn't already at maximum HP.
	/// </summary>
	public static bool CanUseItem(int healAmount, int currentHp, int maxHp)
		=> healAmount > 0 && currentHp < maxHp;

	/// <summary>
	/// True when the item has *any* effect available on the target — heals HP and
	/// the target is below max HP, OR restores MP and the target is below max MP.
	/// Used by the inventory / battle item-use buttons to grey themselves out
	/// correctly when the player is full on whatever the item restores.
	/// </summary>
	public static bool CanUseItem(
		int healAmount, int restoreMp,
		int currentHp,  int maxHp,
		int currentMp,  int maxMp)
	{
		bool canHeal    = healAmount > 0 && currentHp < maxHp;
		bool canRestore = restoreMp  > 0 && currentMp < maxMp;
		return canHeal || canRestore;
	}

	/// <summary>Returns the player's HP after applying the heal, clamped to maxHp.</summary>
	public static int ApplyHeal(int healAmount, int currentHp, int maxHp)
		=> Math.Min(maxHp, currentHp + healAmount);

	/// <summary>
	/// Returns the HP actually restored (may be less than healAmount when the
	/// player is near max HP).
	/// </summary>
	public static int ActualHeal(int healAmount, int currentHp, int maxHp)
		=> ApplyHeal(healAmount, currentHp, maxHp) - currentHp;

	/// <summary>Returns the target MP after applying the restore, clamped to maxMp.</summary>
	public static int ApplyMpRestore(int restoreMp, int currentMp, int maxMp)
		=> Math.Min(maxMp, currentMp + restoreMp);

	/// <summary>
	/// Returns the MP actually restored (may be less than restoreMp when the
	/// target is near max MP).
	/// </summary>
	public static int ActualMpRestore(int restoreMp, int currentMp, int maxMp)
		=> ApplyMpRestore(restoreMp, currentMp, maxMp) - currentMp;
}
