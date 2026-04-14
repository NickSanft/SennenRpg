using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Converts class-specific minigame results into HitGrades and damage numbers.
/// Stateless helper — all methods are static.
/// </summary>
public static class BattleAttackResolver
{
	/// <summary>Convert Fighter timing-bar accuracy to a HitGrade.</summary>
	public static HitGrade ResolveFighterGrade(float accuracy) => accuracy switch
	{
		>= 0.85f => HitGrade.Perfect,
		>= 0.40f => HitGrade.Good,
		_        => HitGrade.Miss,
	};

	/// <summary>Convert Ranger aim accuracy to a HitGrade.</summary>
	public static HitGrade ResolveRangerGrade(float accuracy) => accuracy switch
	{
		>= 0.75f => HitGrade.Perfect,
		>= 0.35f => HitGrade.Good,
		_        => HitGrade.Miss,
	};

	/// <summary>Convert Mage correct-rune count to a HitGrade.</summary>
	public static HitGrade ResolveMageGrade(int correctCount) => correctCount switch
	{
		3 => HitGrade.Perfect,
		2 => HitGrade.Good,
		_ => HitGrade.Miss,
	};

	/// <summary>Compute physical strike damage from grade, stats, and RNG.</summary>
	public static (int damage, bool isCrit, string label) ResolveStrike(
		HitGrade grade, int attack, int defense, int luck)
	{
		float mult   = RhythmConstants.GradeMultiplier(grade);
		bool  isCrit = BattleFormulas.IsCrit(grade == HitGrade.Perfect, luck, GD.Randf());
		int   damage = BattleFormulas.PhysicalDamage(attack, defense, mult);
		if (isCrit) damage *= 2;

		string label = grade switch
		{
			HitGrade.Perfect => "Perfect hit!",
			HitGrade.Good    => "Hit!",
			_                => "Weak hit."
		};

		return (damage, isCrit, label);
	}

	/// <summary>Compute Ranger bull's-eye crit damage (bypasses defence).</summary>
	public static int ResolveRangerCrit(int attack) => attack;

	/// <summary>
	/// Estimate the damage range for a basic Fight attack against an enemy.
	/// The low bound uses the Miss grade multiplier; the high bound uses the Perfect grade
	/// multiplier (non-crit, for a conservative upper bound the player can reliably hit).
	/// Used by the battle HUD damage preview.
	/// </summary>
	public static (int minDmg, int maxDmg) EstimateFightDamageRange(int attack, int enemyDefense)
	{
		int low  = BattleFormulas.PhysicalDamage(attack, enemyDefense,
			RhythmConstants.GradeMultiplier(HitGrade.Miss));
		int high = BattleFormulas.PhysicalDamage(attack, enemyDefense,
			RhythmConstants.GradeMultiplier(HitGrade.Perfect));
		if (high < low) high = low;
		return (low, high);
	}

	/// <summary>Compute spell damage from grade and stats.</summary>
	public static (int damage, bool isCrit) ResolveSpell(
		HitGrade grade, int basePower, int magic, int resistance)
	{
		float mult   = RhythmConstants.GradeMultiplier(grade);
		int   damage = BattleFormulas.MagicDamage(basePower, magic, resistance, mult);
		bool  isCrit = grade == HitGrade.Perfect;
		if (isCrit) damage *= 2;
		return (damage, isCrit);
	}
}
