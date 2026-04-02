using System.Collections.Generic;
using SennenRpg.Autoloads;

namespace SennenRpg.Core.Data;

/// <summary>
/// Holds Mellyr Outpost passive reward state: step counter, Rain gold, and Lily recipe queue.
/// Plain C# class — owned by GameManager as an internal domain.
/// </summary>
public class MellyrRewardData
{
	public int          TownStepCounter    { get; set; } = 0;
	public int          PendingRainGold    { get; set; } = 0;
	public List<string> PendingLilyRecipes { get; }      = new();

	/// <summary>
	/// Transfers all pending Rain gold. Returns the amount collected (0 if nothing was pending).
	/// Does NOT add to player gold — the caller is responsible for that.
	/// </summary>
	public int CollectRainRewards()
	{
		int amount = PendingRainGold;
		PendingRainGold = 0;
		return amount;
	}

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	public void Reset()
	{
		TownStepCounter = 0;
		PendingRainGold = 0;
		PendingLilyRecipes.Clear();
	}

	public void ApplyFromSave(SaveData data)
	{
		TownStepCounter = data.TownStepCounter;
		PendingRainGold = data.PendingRainGold;
		PendingLilyRecipes.Clear();
		PendingLilyRecipes.AddRange(data.PendingLilyRecipes);
	}
}
