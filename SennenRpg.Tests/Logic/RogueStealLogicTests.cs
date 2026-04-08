using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class RogueStealLogicTests
{
    [Test]
    public void Resolve_AllPerfects_TriggersStealAndCrit()
    {
        var outcome = RogueStealLogic.Resolve(perfectCount: 3, hitCount: 3);
        Assert.That(outcome, Is.EqualTo(RogueStrikeOutcome.PerfectSteal));
        Assert.That(RogueStealLogic.GuaranteedCrit(outcome), Is.True);
        Assert.That(RogueStealLogic.ShouldSteal(outcome),    Is.True);
        Assert.That(RogueStealLogic.ToHitGrade(outcome),     Is.EqualTo(HitGrade.Perfect));
    }

    [Test]
    public void Resolve_ThreeHitsZeroPerfects_IsPerfectNoSteal()
    {
        var outcome = RogueStealLogic.Resolve(perfectCount: 0, hitCount: 3);
        Assert.That(outcome, Is.EqualTo(RogueStrikeOutcome.Perfect));
        Assert.That(RogueStealLogic.ShouldSteal(outcome), Is.False);
        Assert.That(RogueStealLogic.GuaranteedCrit(outcome), Is.False);
        Assert.That(RogueStealLogic.ToHitGrade(outcome), Is.EqualTo(HitGrade.Perfect));
    }

    [Test]
    public void Resolve_TwoHits_IsGood()
    {
        var outcome = RogueStealLogic.Resolve(perfectCount: 1, hitCount: 2);
        Assert.That(outcome, Is.EqualTo(RogueStrikeOutcome.Good));
        Assert.That(RogueStealLogic.ToHitGrade(outcome), Is.EqualTo(HitGrade.Good));
    }

    [Test]
    public void Resolve_OneHit_IsWeakHit()
    {
        var outcome = RogueStealLogic.Resolve(perfectCount: 0, hitCount: 1);
        Assert.That(outcome, Is.EqualTo(RogueStrikeOutcome.WeakHit));
        Assert.That(RogueStealLogic.ToHitGrade(outcome), Is.EqualTo(HitGrade.Good));
    }

    [Test]
    public void Resolve_NoHits_IsMiss()
    {
        var outcome = RogueStealLogic.Resolve(perfectCount: 0, hitCount: 0);
        Assert.That(outcome, Is.EqualTo(RogueStrikeOutcome.Miss));
        Assert.That(RogueStealLogic.ToHitGrade(outcome), Is.EqualTo(HitGrade.Miss));
    }

    [Test]
    public void Resolve_ClampsNegativeAndOverflow()
    {
        Assert.That(RogueStealLogic.Resolve(-1, -1), Is.EqualTo(RogueStrikeOutcome.Miss));
        Assert.That(RogueStealLogic.Resolve(99, 99), Is.EqualTo(RogueStrikeOutcome.PerfectSteal));
    }

    [Test]
    public void Resolve_PerfectCannotExceedHits()
    {
        // perfectCount > hitCount is nonsense input — should be clamped down
        var outcome = RogueStealLogic.Resolve(perfectCount: 3, hitCount: 1);
        Assert.That(outcome, Is.EqualTo(RogueStrikeOutcome.WeakHit));
    }
}
