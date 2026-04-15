using NUnit.Framework;
using SennenRpg.Core.Data;
using SennenRpg.Scenes.Battle;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class BattleAttackResolverTests
{
	// ── EstimateFightDamageRange ──────────────────────────────────────────────

	[Test]
	public void EstimateFightDamageRange_NormalInputs_MinLessThanMax()
	{
		var (min, max) = BattleAttackResolver.EstimateFightDamageRange(attack: 20, enemyDefense: 5);
		Assert.That(min, Is.LessThan(max));
		Assert.That(min, Is.GreaterThanOrEqualTo(1));
	}

	[Test]
	public void EstimateFightDamageRange_AttackFarBelowDefense_MinClampsToOne()
	{
		// attack=1, defense=100: even Perfect (1.5*) = ~2 - 100 → clamp to 1
		var (min, max) = BattleAttackResolver.EstimateFightDamageRange(attack: 1, enemyDefense: 100);
		Assert.That(min, Is.EqualTo(1));
		Assert.That(max, Is.EqualTo(1));
		Assert.That(max, Is.GreaterThanOrEqualTo(min));
	}

	[Test]
	public void EstimateFightDamageRange_ZeroAttack_BothClampToOne()
	{
		// PhysicalDamage clamps to minimum of 1, so attack=0 still yields 1 (not 0).
		var (min, max) = BattleAttackResolver.EstimateFightDamageRange(attack: 0, enemyDefense: 0);
		Assert.That(min, Is.EqualTo(1));
		Assert.That(max, Is.EqualTo(1));
	}

	[Test]
	public void EstimateFightDamageRange_IncreasesMonotonicallyWithAttack()
	{
		var (_, max10) = BattleAttackResolver.EstimateFightDamageRange(10, 0);
		var (_, max20) = BattleAttackResolver.EstimateFightDamageRange(20, 0);
		var (_, max40) = BattleAttackResolver.EstimateFightDamageRange(40, 0);
		Assert.That(max20, Is.GreaterThan(max10));
		Assert.That(max40, Is.GreaterThan(max20));
	}

	[Test]
	public void EstimateFightDamageRange_HighDefense_ShrinksRange()
	{
		var (minLow,  maxLow)  = BattleAttackResolver.EstimateFightDamageRange(attack: 50, enemyDefense: 0);
		var (minHigh, maxHigh) = BattleAttackResolver.EstimateFightDamageRange(attack: 50, enemyDefense: 40);
		int rangeLow  = maxLow  - minLow;
		int rangeHigh = maxHigh - minHigh;
		Assert.That(rangeHigh, Is.LessThanOrEqualTo(rangeLow));
	}

	[Test]
	public void EstimateFightDamageRange_MaxNeverLessThanMin()
	{
		// Edge: guarantee high >= low even in pathological clamp situations
		for (int def = 0; def < 200; def += 25)
		{
			var (min, max) = BattleAttackResolver.EstimateFightDamageRange(attack: 10, enemyDefense: def);
			Assert.That(max, Is.GreaterThanOrEqualTo(min), $"defense={def}");
		}
	}

	// ── ResolveFighterGrade ───────────────────────────────────────────────────

	[Test]
	public void ResolveFighterGrade_HighAccuracy_Perfect()
	{
		Assert.That(BattleAttackResolver.ResolveFighterGrade(0.90f), Is.EqualTo(HitGrade.Perfect));
		Assert.That(BattleAttackResolver.ResolveFighterGrade(0.85f), Is.EqualTo(HitGrade.Perfect));
	}

	[Test]
	public void ResolveFighterGrade_MidAccuracy_Good()
	{
		Assert.That(BattleAttackResolver.ResolveFighterGrade(0.84f), Is.EqualTo(HitGrade.Good));
		Assert.That(BattleAttackResolver.ResolveFighterGrade(0.40f), Is.EqualTo(HitGrade.Good));
	}

	[Test]
	public void ResolveFighterGrade_LowAccuracy_Miss()
	{
		Assert.That(BattleAttackResolver.ResolveFighterGrade(0.39f), Is.EqualTo(HitGrade.Miss));
		Assert.That(BattleAttackResolver.ResolveFighterGrade(0f),    Is.EqualTo(HitGrade.Miss));
	}

	// ── ResolveRangerGrade ────────────────────────────────────────────────────

	[Test]
	public void ResolveRangerGrade_Boundaries()
	{
		Assert.That(BattleAttackResolver.ResolveRangerGrade(0.75f), Is.EqualTo(HitGrade.Perfect));
		Assert.That(BattleAttackResolver.ResolveRangerGrade(0.74f), Is.EqualTo(HitGrade.Good));
		Assert.That(BattleAttackResolver.ResolveRangerGrade(0.35f), Is.EqualTo(HitGrade.Good));
		Assert.That(BattleAttackResolver.ResolveRangerGrade(0.34f), Is.EqualTo(HitGrade.Miss));
		Assert.That(BattleAttackResolver.ResolveRangerGrade(-1f),   Is.EqualTo(HitGrade.Miss));
	}

	// ── ResolveMageGrade ──────────────────────────────────────────────────────

	[Test]
	public void ResolveMageGrade_Mapping()
	{
		Assert.That(BattleAttackResolver.ResolveMageGrade(3), Is.EqualTo(HitGrade.Perfect));
		Assert.That(BattleAttackResolver.ResolveMageGrade(2), Is.EqualTo(HitGrade.Good));
		Assert.That(BattleAttackResolver.ResolveMageGrade(1), Is.EqualTo(HitGrade.Miss));
		Assert.That(BattleAttackResolver.ResolveMageGrade(0), Is.EqualTo(HitGrade.Miss));
		Assert.That(BattleAttackResolver.ResolveMageGrade(4), Is.EqualTo(HitGrade.Miss));
	}

	// ── ResolveRangerCrit ─────────────────────────────────────────────────────

	[Test]
	public void ResolveRangerCrit_ReturnsAttack()
	{
		Assert.That(BattleAttackResolver.ResolveRangerCrit(0),   Is.EqualTo(0));
		Assert.That(BattleAttackResolver.ResolveRangerCrit(50),  Is.EqualTo(50));
		Assert.That(BattleAttackResolver.ResolveRangerCrit(999), Is.EqualTo(999));
	}

	// ── ResolveSpell ──────────────────────────────────────────────────────────

	[Test]
	public void ResolveSpell_PerfectGrade_IsCritAndDoubles()
	{
		var (dmgPerfect, critPerfect) = BattleAttackResolver.ResolveSpell(
			HitGrade.Perfect, basePower: 20, magic: 20, resistance: 0);
		var (dmgGood, critGood) = BattleAttackResolver.ResolveSpell(
			HitGrade.Good, basePower: 20, magic: 20, resistance: 0);

		Assert.That(critPerfect, Is.True);
		Assert.That(critGood,    Is.False);
		Assert.That(dmgPerfect,  Is.GreaterThan(dmgGood));
	}

	[Test]
	public void ResolveSpell_MissGrade_StillAtLeastOne()
	{
		var (dmg, crit) = BattleAttackResolver.ResolveSpell(
			HitGrade.Miss, basePower: 1, magic: 1, resistance: 100);
		Assert.That(dmg, Is.GreaterThanOrEqualTo(1));
		Assert.That(crit, Is.False);
	}

	[Test]
	public void ResolveSpell_DamageIncreasesWithMagic()
	{
		var (dmgLow,  _) = BattleAttackResolver.ResolveSpell(HitGrade.Good, 20, magic: 10, resistance: 0);
		var (dmgHigh, _) = BattleAttackResolver.ResolveSpell(HitGrade.Good, 20, magic: 50, resistance: 0);
		Assert.That(dmgHigh, Is.GreaterThan(dmgLow));
	}
}
