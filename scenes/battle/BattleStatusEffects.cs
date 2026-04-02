using Godot;
using System.Collections.Generic;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Manages battle status effects (poison, stun, etc.) for both player and enemy.
/// Encapsulates status dictionaries, tick logic, and Dialogic signal parsing.
/// </summary>
public class BattleStatusEffects
{
	private readonly Dictionary<StatusEffect, int> _playerStatuses = new();
	private readonly Dictionary<StatusEffect, int> _enemyStatuses  = new();

	public Dictionary<StatusEffect, int> PlayerStatuses => _playerStatuses;
	public Dictionary<StatusEffect, int> EnemyStatuses  => _enemyStatuses;

	/// <summary>Tick down all status durations for both sides. Call at the start of each round.</summary>
	public void TickAll()
	{
		StatusLogic.TickAll(_playerStatuses);
		StatusLogic.TickAll(_enemyStatuses);
	}

	public bool PlayerHasStatus(StatusEffect effect) => StatusLogic.HasStatus(_playerStatuses, effect);
	public bool EnemyHasStatus(StatusEffect effect)  => StatusLogic.HasStatus(_enemyStatuses, effect);

	/// <summary>
	/// Handles "status:effect:turns" Dialogic signals.
	/// Returns true if a status was applied.
	/// </summary>
	public bool TryHandleDialogicSignal(string signalText)
	{
		var (type, arg) = DialogicSignalParser.Parse(signalText);
		if (type != DialogicSignalParser.TypeStatus) return false;

		if (StatusLogic.TryParseStatusSignal(arg, out StatusEffect effect, out int turns))
		{
			StatusLogic.Apply(_playerStatuses, effect, turns);
			GD.Print($"[BattleStatusEffects] Status applied to player: {effect} for {turns} turns");
			return true;
		}
		return false;
	}

	/// <summary>Compute poison damage for the player.</summary>
	public int PlayerPoisonDamage(int maxHp) => StatusLogic.PoisonDamage(maxHp);

	/// <summary>Compute poison damage for the enemy.</summary>
	public int EnemyPoisonDamage(int maxHp) => StatusLogic.PoisonDamage(maxHp);
}
