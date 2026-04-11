using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

/// <summary>
/// NUnit tests for SkillResolver — Phase 4 unique skills (Bhata Gravity Arrow,
/// Rain Dual-Class, Lily Wither and Bloom).
/// </summary>
[TestFixture]
public sealed class SkillResolverTests
{
    // ── Ranger-style skills (Gravity Arrow / Dual-Class) ─────────────────────

    [Test]
    public void GravityArrow_DoublesBaseAttackBeforeDefence()
    {
        // 20 atk × 2 = 40, − 10 def = 30, × 1.0 accuracy → 30
        int dmg = SkillResolver.ResolveRangerSkillDamage(
            actorAttack: 20, targetDefence: 10, accuracy: 1f,
            multiplier: SkillResolver.GravityArrowMultiplier);
        Assert.That(dmg, Is.EqualTo(30));
    }

    [Test]
    public void DualClass_HalfAccuracyHalvesDamage()
    {
        // 15 atk × 2 = 30, − 5 def = 25, × 0.5 acc → 12
        int dmg = SkillResolver.ResolveRangerSkillDamage(
            actorAttack: 15, targetDefence: 5, accuracy: 0.5f,
            multiplier: SkillResolver.DualClassMultiplier);
        Assert.That(dmg, Is.EqualTo(12));
    }

    [Test]
    public void RangerSkill_ZeroAccuracyStillDealsAtLeastOne()
    {
        int dmg = SkillResolver.ResolveRangerSkillDamage(
            actorAttack: 20, targetDefence: 5, accuracy: 0f, multiplier: 2f);
        Assert.That(dmg, Is.EqualTo(1));
    }

    [Test]
    public void RangerSkill_OverpoweredDefenceClampedToOnePreMultiplier()
    {
        // 5 atk × 2 = 10, − 100 def → 1 (clamped), × 1.0 → 1
        int dmg = SkillResolver.ResolveRangerSkillDamage(
            actorAttack: 5, targetDefence: 100, accuracy: 1f, multiplier: 2f);
        Assert.That(dmg, Is.EqualTo(1));
    }

    [Test]
    public void RangerSkill_NegativeAccuracyIsClampedToZero()
    {
        int dmg = SkillResolver.ResolveRangerSkillDamage(20, 5, accuracy: -0.5f, multiplier: 2f);
        Assert.That(dmg, Is.EqualTo(1));
    }

    // ── Wither and Bloom — damage ────────────────────────────────────────────

    [Test]
    public void WitherDamage_FullFillScalesWithMagic()
    {
        // 20 mag × (0.75 + 1.0) = 35, − 5 def = 30
        int dmg = SkillResolver.ResolveWitherDamage(actorMagic: 20, targetDefence: 5, fillRatio: 1f);
        Assert.That(dmg, Is.EqualTo(30));
    }

    [Test]
    public void WitherDamage_ZeroFillStillDealsBaseChunk()
    {
        // 20 × 0.75 = 15, − 0 = 15
        int dmg = SkillResolver.ResolveWitherDamage(20, 0, 0f);
        Assert.That(dmg, Is.EqualTo(15));
    }

    [Test]
    public void WitherDamage_ClampsToOneAgainstHugeDefence()
    {
        int dmg = SkillResolver.ResolveWitherDamage(10, 100, 1f);
        Assert.That(dmg, Is.EqualTo(1));
    }

    // ── Wither and Bloom — heal pool & split ────────────────────────────────

    [Test]
    public void WitherHealPool_ScalesWithFillRatio()
    {
        // 20 × (0.5 + 1.0) = 30
        Assert.That(SkillResolver.ResolveWitherHealPool(20, 1f), Is.EqualTo(30));
    }

    [Test]
    public void WitherHealPool_ZeroFillStillProducesPool()
    {
        // 20 × 0.5 = 10
        Assert.That(SkillResolver.ResolveWitherHealPool(20, 0f), Is.EqualTo(10));
    }

    [Test]
    public void SplitHealEvenly_DividesAcrossLivingMembers()
        => Assert.That(SkillResolver.SplitHealEvenly(30, 3), Is.EqualTo(10));

    [Test]
    public void SplitHealEvenly_RoundsDown()
        => Assert.That(SkillResolver.SplitHealEvenly(31, 3), Is.EqualTo(10));

    [Test]
    public void SplitHealEvenly_ReturnsZeroOnEmptyParty()
        => Assert.That(SkillResolver.SplitHealEvenly(30, 0), Is.EqualTo(0));

    [Test]
    public void SplitHealEvenly_FloorsToOneIfAnyPoolExists()
        => Assert.That(SkillResolver.SplitHealEvenly(2, 5), Is.EqualTo(1));

    // ── Constants sanity ─────────────────────────────────────────────────────

    // ── Crystal Knife — Kriora AoE skill ────────────────────────────────────

    [Test]
    public void CrystalKnife_PerfectCountGivesFullMultiplier()
    {
        // 16 mag × 2.0 × 1.0 = 32, − 5 def = 27
        int dmg = SkillResolver.ResolveCrystalKnifeDamage(actorMagic: 16, targetDefence: 5, correctCount: 3);
        Assert.That(dmg, Is.EqualTo(27));
    }

    [Test]
    public void CrystalKnife_DamageScalesWithCorrectCount()
    {
        int dmg3 = SkillResolver.ResolveCrystalKnifeDamage(20, 2, 3);
        int dmg2 = SkillResolver.ResolveCrystalKnifeDamage(20, 2, 2);
        int dmg1 = SkillResolver.ResolveCrystalKnifeDamage(20, 2, 1);
        int dmg0 = SkillResolver.ResolveCrystalKnifeDamage(20, 2, 0);
        Assert.That(dmg3, Is.GreaterThan(dmg2));
        Assert.That(dmg2, Is.GreaterThan(dmg1));
        Assert.That(dmg1, Is.GreaterThan(dmg0));
    }

    [Test]
    public void CrystalKnife_NeverBelowOne()
    {
        int dmg = SkillResolver.ResolveCrystalKnifeDamage(actorMagic: 1, targetDefence: 999, correctCount: 0);
        Assert.That(dmg, Is.EqualTo(1));
    }

    [Test]
    public void CrystalKnife_ZeroCorrectStillDeals()
    {
        // 20 × 2.0 × 0.4 = 16, − 2 = 14
        int dmg = SkillResolver.ResolveCrystalKnifeDamage(20, 2, 0);
        Assert.That(dmg, Is.EqualTo(14));
    }

    // ── Constants sanity ─────────────────────────────────────────────────────

    [Test]
    public void MpCosts_AreReasonable()
    {
        Assert.That(SkillResolver.GravityArrowMpCost,   Is.EqualTo(6));
        Assert.That(SkillResolver.DualClassMpCost,      Is.EqualTo(6));
        Assert.That(SkillResolver.WitherAndBloomMpCost, Is.EqualTo(8));
        Assert.That(SkillResolver.CrystalKnifeMpCost,   Is.EqualTo(10));
    }
}
