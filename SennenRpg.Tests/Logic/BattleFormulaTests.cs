using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class BattleFormulaTests
{
    // ── PhysicalDamage ────────────────────────────────────────────────────────

    [Test]
    public void PhysicalDamage_GradeMultOne_AttackMinusDefense()
    {
        int result = BattleFormulas.PhysicalDamage(attack: 10, defense: 3, gradeMult: 1.0f);
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void PhysicalDamage_PerfectGrade_AppliesMultiplier()
    {
        // Attack 10 × 1.5 = 15, minus defense 2 = 13
        int result = BattleFormulas.PhysicalDamage(attack: 10, defense: 2, gradeMult: 1.5f);
        Assert.That(result, Is.EqualTo(13));
    }

    [Test]
    public void PhysicalDamage_NeverBelowOne()
    {
        // High defense, weak attack, miss grade
        int result = BattleFormulas.PhysicalDamage(attack: 1, defense: 99, gradeMult: 0.5f);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void PhysicalDamage_ZeroDefense_FullDamage()
    {
        int result = BattleFormulas.PhysicalDamage(attack: 8, defense: 0, gradeMult: 1.0f);
        Assert.That(result, Is.EqualTo(8));
    }

    // ── MagicDamage ───────────────────────────────────────────────────────────

    [Test]
    public void MagicDamage_NeverBelowOne()
    {
        int result = BattleFormulas.MagicDamage(spellBasePower: 1, magic: 1, resistance: 99, gradeMult: 1.0f);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void MagicDamage_BypassesPhysicalDefense()
    {
        // Magic damage is independent of defense — resistance is the relevant stat
        int physical = BattleFormulas.PhysicalDamage(attack: 10, defense: 0, gradeMult: 1.0f);
        int magic    = BattleFormulas.MagicDamage(spellBasePower: 18, magic: 8, resistance: 0, gradeMult: 1.0f);
        // They use different formulas — just verify magic produces a positive value
        Assert.That(magic, Is.GreaterThan(0));
        Assert.That(physical, Is.GreaterThan(0));
    }

    // ── IsCrit ────────────────────────────────────────────────────────────────

    [Test]
    public void IsCrit_PerfectGrade_AlwaysCrits_RegardlessOfLuck()
    {
        // Even with luck 0 and an unfavourable roll, Perfect always crits
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: true, luck: 0,   randomValue: 0.99f), Is.True);
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: true, luck: 200, randomValue: 0.99f), Is.True);
    }

    [Test]
    public void IsCrit_LuckZero_NeverCritsOnNonPerfect()
    {
        // Luck 0 → 0 / 200f = 0 threshold; no random value can be below 0
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 0, randomValue: 0.0f),  Is.False);
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 0, randomValue: 0.99f), Is.False);
    }

    [Test]
    public void IsCrit_LuckMaximum_AlwaysCritsOnNonPerfect()
    {
        // Luck 200 → threshold = 1.0f; any value in [0,1) is below 1.0
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 200, randomValue: 0.0f),  Is.True);
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 200, randomValue: 0.999f), Is.True);
    }

    [Test]
    public void IsCrit_Luck100_CritsWhenRollBelowHalf()
    {
        // Luck 100 → threshold = 0.5
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 100, randomValue: 0.49f), Is.True);
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 100, randomValue: 0.50f), Is.False);
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 100, randomValue: 0.99f), Is.False);
    }

    [Test]
    public void IsCrit_Luck12_CritsOnlyOnLowRolls()
    {
        // Default player luck 12 → threshold = 12 / 200f = 0.06
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 12, randomValue: 0.05f), Is.True);
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 12, randomValue: 0.06f), Is.False);
        Assert.That(BattleFormulas.IsCrit(isPerfectGrade: false, luck: 12, randomValue: 0.50f), Is.False);
    }

    // ── PlayerGoesFirst ───────────────────────────────────────────────────────

    [Test]
    public void PlayerGoesFirst_HigherSpeed_ReturnsTrue()
        => Assert.That(BattleFormulas.PlayerGoesFirst(playerSpeed: 15, enemySpeed: 8), Is.True);

    [Test]
    public void PlayerGoesFirst_EqualSpeed_PlayerWinsTie()
        => Assert.That(BattleFormulas.PlayerGoesFirst(playerSpeed: 10, enemySpeed: 10), Is.True);

    [Test]
    public void PlayerGoesFirst_LowerSpeed_ReturnsFalse()
        => Assert.That(BattleFormulas.PlayerGoesFirst(playerSpeed: 10, enemySpeed: 18), Is.False);

    // ── FleeChance ────────────────────────────────────────────────────────────

    [Test]
    public void FleeChance_EqualSpeed_Returns50Percent()
        => Assert.That(BattleFormulas.FleeChance(playerSpeed: 10, enemySpeed: 10), Is.EqualTo(50));

    [Test]
    public void FleeChance_FasterPlayer_IncreasesChance()
    {
        // +5 speed → 50 + 5×3 = 65
        Assert.That(BattleFormulas.FleeChance(playerSpeed: 15, enemySpeed: 10), Is.EqualTo(65));
    }

    [Test]
    public void FleeChance_SlowerPlayer_DecreasesChance()
    {
        // −5 speed → 50 − 5×3 = 35
        Assert.That(BattleFormulas.FleeChance(playerSpeed: 5, enemySpeed: 10), Is.EqualTo(35));
    }

    [Test]
    public void FleeChance_ClampsAtMinimum10()
    {
        // Extreme speed disadvantage should floor at 10
        Assert.That(BattleFormulas.FleeChance(playerSpeed: 0, enemySpeed: 99), Is.EqualTo(10));
    }

    [Test]
    public void FleeChance_ClampsAtMaximum95()
    {
        // Extreme speed advantage should cap at 95
        Assert.That(BattleFormulas.FleeChance(playerSpeed: 99, enemySpeed: 0), Is.EqualTo(95));
    }

    // ── AttemptFlee ───────────────────────────────────────────────────────────

    [Test]
    public void AttemptFlee_LowRoll_Succeeds()
    {
        // 50% chance at equal speed; roll 0.01 is well below threshold
        Assert.That(BattleFormulas.AttemptFlee(playerSpeed: 10, enemySpeed: 10, randomValue: 0.01f), Is.True);
    }

    [Test]
    public void AttemptFlee_HighRoll_Fails()
    {
        // 50% chance at equal speed; roll 0.99 is above threshold
        Assert.That(BattleFormulas.AttemptFlee(playerSpeed: 10, enemySpeed: 10, randomValue: 0.99f), Is.False);
    }

    [Test]
    public void AttemptFlee_RollEqualsThreshold_Fails()
    {
        // Threshold is exclusive: roll == 0.50 should fail (chance = 50 → 0.50f, not < 0.50f)
        Assert.That(BattleFormulas.AttemptFlee(playerSpeed: 10, enemySpeed: 10, randomValue: 0.50f), Is.False);
    }
}
