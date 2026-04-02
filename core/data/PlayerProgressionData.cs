using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Holds player economy and progression state: gold, experience, level, and play time.
/// Plain C# class — not a Godot node or resource. Owned by GameManager as an internal domain.
/// </summary>
public class PlayerProgressionData
{
	public int Gold            { get; private set; }
	public int Exp             { get; private set; }
	public int PlayerLevel     { get; private set; } = 1;
	public int PlayTimeSeconds { get; private set; } = 0;

	/// <summary>Results from the most recent level-up batch. Read and clear after showing the screen.</summary>
	public List<LevelUpResult> PendingLevelUps { get; } = new();

	private double _playTimeAccumulator = 0.0;

	// ── Economy ──────────────────────────────────────────────────────────────

	public void AddGold(int amount)    => Gold += amount;
	public void RemoveGold(int amount) => Gold  = System.Math.Max(0, Gold - amount);

	// ── Progression ───────────────────────────────────────────────────────────

	public void AddExp(int amount)  => Exp += amount;
	public void IncrementLevel()    => PlayerLevel++;

	// ── Play time ─────────────────────────────────────────────────────────────

	/// <summary>
	/// Accumulates play time. Call every frame while in an active game state;
	/// increments <see cref="PlayTimeSeconds"/> in whole-second steps.
	/// </summary>
	public void Tick(double delta)
	{
		_playTimeAccumulator += delta;
		if (_playTimeAccumulator >= 1.0)
		{
			int whole             = (int)_playTimeAccumulator;
			PlayTimeSeconds      += whole;
			_playTimeAccumulator -= whole;
		}
	}

	// ── Lifecycle ─────────────────────────────────────────────────────────────

	public void Reset()
	{
		Gold                 = 500;
		Exp                  = 0;
		PlayerLevel          = 1;
		PlayTimeSeconds      = 0;
		_playTimeAccumulator = 0.0;
		PendingLevelUps.Clear();
	}

	public void ApplyFromSave(int gold, int exp, int level, int playTime)
	{
		Gold                 = gold;
		Exp                  = exp;
		PlayerLevel          = level > 0 ? level : 1;
		PlayTimeSeconds      = playTime;
		_playTimeAccumulator = 0.0;
		PendingLevelUps.Clear();
	}
}
