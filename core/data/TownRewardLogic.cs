namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static logic for the Mellyr Outpost passive reward system.
/// Called every step on 16×16 maps (WorldMap, DungeonFloor*).
/// Fully testable without a Godot runtime.
/// </summary>
public static class TownRewardLogic
{
	public const int TickEvery           = 10;
	public const int RainGoldPerTick     = 10;
	public const int MaxPendingRainGold  = 200;
	public const int MaxPendingLilyItems = 5;
	public const int MaxPendingBhataAles = 5;

	/// <summary>
	/// Processes one step on a qualifying map.
	/// Returns a <see cref="TownTickResult"/> describing the new state and what (if anything) triggered.
	/// The caller is responsible for writing the new values back to GameManager.
	/// </summary>
	public static TownTickResult TryTick(
		int  stepCounter,
		bool rainPurchased,
		int  pendingRainGold,
		bool lilyPurchased,
		int  pendingLilyCount,
		int  playerLevel,
		bool bhataPurchased    = false,
		int  pendingBhataAles  = 0)
	{
		stepCounter++;
		if (stepCounter < TickEvery)
			return new TownTickResult(stepCounter, pendingRainGold, pendingLilyCount, pendingBhataAles, false, false, false, null);

		// Reset counter
		stepCounter = 0;

		bool rainTicked  = false;
		bool lilyTicked  = false;
		bool bhataTicked = false;
		string? lilyRecipe = null;

		if (rainPurchased && pendingRainGold < MaxPendingRainGold)
		{
			pendingRainGold = System.Math.Min(pendingRainGold + RainGoldPerTick, MaxPendingRainGold);
			rainTicked = true;
		}

		if (lilyPurchased && pendingLilyCount < MaxPendingLilyItems)
		{
			lilyRecipe = LilyForgeLogic.GenerateRecipe(playerLevel);
			pendingLilyCount++;
			lilyTicked = true;
		}

		if (bhataPurchased && pendingBhataAles < MaxPendingBhataAles)
		{
			pendingBhataAles++;
			bhataTicked = true;
		}

		return new TownTickResult(stepCounter, pendingRainGold, pendingLilyCount, pendingBhataAles, rainTicked, lilyTicked, bhataTicked, lilyRecipe);
	}

	/// <summary>Returns true if any resident has rewards waiting to be collected.</summary>
	public static bool HasPendingRewards(int pendingRainGold, int pendingLilyCount, int pendingBhataAles = 0)
		=> pendingRainGold > 0 || pendingLilyCount > 0 || pendingBhataAles > 0;
}

/// <summary>Result of a single <see cref="TownRewardLogic.TryTick"/> call.</summary>
public record TownTickResult(
	int     NewCounter,
	int     NewPendingRainGold,
	int     NewPendingLilyCount,
	int     NewPendingBhataAles,
	bool    RainTicked,
	bool    LilyTicked,
	bool    BhataTicked,
	string? LilyRecipe);
