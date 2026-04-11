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
	public int          PendingBhataAles   { get; set; } = 0;
	public List<string> PendingKrioraRecipes { get; }    = new();

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

	/// <summary>
	/// Transfers all pending Bhata ales. Returns the count collected.
	/// Does NOT add items to inventory — the caller is responsible for that.
	/// </summary>
	public int CollectBhataRewards()
	{
		int count = PendingBhataAles;
		PendingBhataAles = 0;
		return count;
	}

	/// <summary>
	/// Transfers all pending Kriora crystal weapon recipes. Returns the recipe list.
	/// Does NOT resolve or add items — the caller is responsible for that.
	/// </summary>
	public List<string> CollectKrioraRewards()
	{
		var recipes = new List<string>(PendingKrioraRecipes);
		PendingKrioraRecipes.Clear();
		return recipes;
	}

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	public void Reset()
	{
		TownStepCounter = 0;
		PendingRainGold = 0;
		PendingLilyRecipes.Clear();
		PendingBhataAles = 0;
		PendingKrioraRecipes.Clear();
	}

	public void ApplyFromSave(SaveData data)
	{
		TownStepCounter = data.TownStepCounter;
		PendingRainGold = data.PendingRainGold;
		PendingLilyRecipes.Clear();
		PendingLilyRecipes.AddRange(data.PendingLilyRecipes);
		PendingBhataAles = data.PendingBhataAles;
		PendingKrioraRecipes.Clear();
		PendingKrioraRecipes.AddRange(data.PendingKrioraRecipes);
	}
}
