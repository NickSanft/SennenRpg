using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class ForageLogicGradeTests
{
    // ── GradeFromAccuracy ────────────────────────────────────────────────

    [Test]
    public void Grade_AllPerfect_ReturnsPerfect()
    {
        Assert.That(ForageLogic.GradeFromAccuracy(5, 5, 5),
            Is.EqualTo(ForageLogic.ForageGrade.Perfect));
    }

    [Test]
    public void Grade_AllHitButNotAllPerfect_ReturnsGreat()
    {
        // 5/5 hit, 3 perfect (60%) → Great (not Perfect, since not 100% perfect)
        Assert.That(ForageLogic.GradeFromAccuracy(5, 3, 5),
            Is.EqualTo(ForageLogic.ForageGrade.Great));
    }

    [Test]
    public void Grade_EightyPercentHitWithPerfectMajority_ReturnsGreat()
    {
        // 4/5 hit, 3 perfect (75% perfect) → Great
        Assert.That(ForageLogic.GradeFromAccuracy(4, 3, 5),
            Is.EqualTo(ForageLogic.ForageGrade.Great));
    }

    [Test]
    public void Grade_EightyPercentHitWithFewPerfects_ReturnsGood()
    {
        // 4/5 hit, 1 perfect (25% perfect) → fails Great's perfectRatio gate → Good
        Assert.That(ForageLogic.GradeFromAccuracy(4, 1, 5),
            Is.EqualTo(ForageLogic.ForageGrade.Good));
    }

    [Test]
    public void Grade_FiftyPercentHit_ReturnsGood()
    {
        // 3/5 hit, 0 perfect → Good
        Assert.That(ForageLogic.GradeFromAccuracy(3, 0, 5),
            Is.EqualTo(ForageLogic.ForageGrade.Good));
    }

    [Test]
    public void Grade_BelowFiftyPercent_ReturnsMiss()
    {
        // 2/5 hit (40%) → Miss
        Assert.That(ForageLogic.GradeFromAccuracy(2, 0, 5),
            Is.EqualTo(ForageLogic.ForageGrade.Miss));
    }

    [Test]
    public void Grade_ZeroNotes_ReturnsMiss()
    {
        Assert.That(ForageLogic.GradeFromAccuracy(0, 0, 0),
            Is.EqualTo(ForageLogic.ForageGrade.Miss));
    }

    [Test]
    public void Grade_NegativeInputs_ClampedSafely()
    {
        // Defensive: garbage inputs should not throw or grade above Miss/Good baseline.
        Assert.That(ForageLogic.GradeFromAccuracy(-1, -1, 5),
            Is.EqualTo(ForageLogic.ForageGrade.Miss));
    }

    [Test]
    public void Grade_PerfectsCappedToHits()
    {
        // 3 hits, but perfects=10 → perfects clamped to 3, then 3/3 == 100% perfect,
        // but hits != totalNotes (3 != 5) so this is Great at best.
        var grade = ForageLogic.GradeFromAccuracy(3, 10, 5);
        Assert.That(grade, Is.EqualTo(ForageLogic.ForageGrade.Good)
            .Or.EqualTo(ForageLogic.ForageGrade.Great));
    }

    // ── BonusItemCount ────────────────────────────────────────────────────

    [Test]
    public void BonusItemCount_MissReturnsOne_NeverWorseThanLegacy()
    {
        Assert.That(ForageLogic.BonusItemCount(ForageLogic.ForageGrade.Miss), Is.EqualTo(1));
    }

    [Test]
    public void BonusItemCount_GoodReturnsOne()
    {
        Assert.That(ForageLogic.BonusItemCount(ForageLogic.ForageGrade.Good), Is.EqualTo(1));
    }

    [Test]
    public void BonusItemCount_GreatReturnsTwo()
    {
        Assert.That(ForageLogic.BonusItemCount(ForageLogic.ForageGrade.Great), Is.EqualTo(2));
    }

    [Test]
    public void BonusItemCount_PerfectReturnsThree()
    {
        Assert.That(ForageLogic.BonusItemCount(ForageLogic.ForageGrade.Perfect), Is.EqualTo(3));
    }

    // ── WeightedTableForGrade ─────────────────────────────────────────────

    [Test]
    public void WeightedTable_Miss_MatchesDefault()
    {
        var biased = ForageLogic.WeightedTableForGrade(ForageLogic.ForageGrade.Miss);
        Assert.That(biased, Has.Length.EqualTo(ForageLogic.DefaultTable.Length));
        for (int i = 0; i < biased.Length; i++)
        {
            Assert.That(biased[i].ItemPath, Is.EqualTo(ForageLogic.DefaultTable[i].ItemPath));
            Assert.That(biased[i].Weight,   Is.EqualTo(ForageLogic.DefaultTable[i].Weight));
        }
    }

    [Test]
    public void WeightedTable_Good_MatchesDefault()
    {
        var biased = ForageLogic.WeightedTableForGrade(ForageLogic.ForageGrade.Good);
        for (int i = 0; i < biased.Length; i++)
            Assert.That(biased[i].Weight, Is.EqualTo(ForageLogic.DefaultTable[i].Weight));
    }

    [Test]
    public void WeightedTable_Great_DoublesRarestHalf()
    {
        var biased = ForageLogic.WeightedTableForGrade(ForageLogic.ForageGrade.Great);
        // Default: [40, 30, 20, 10] — rare half is indices 2, 3 → [40, 30, 40, 20]
        Assert.That(biased[0].Weight, Is.EqualTo(40));
        Assert.That(biased[1].Weight, Is.EqualTo(30));
        Assert.That(biased[2].Weight, Is.EqualTo(40));
        Assert.That(biased[3].Weight, Is.EqualTo(20));
    }

    [Test]
    public void WeightedTable_Perfect_StacksRarestEntryBoost()
    {
        var biased = ForageLogic.WeightedTableForGrade(ForageLogic.ForageGrade.Perfect);
        // Default rarest=10 → ×2 (Great) = 20 → ×1.5 (Perfect, ceiling) = 30
        Assert.That(biased[0].Weight, Is.EqualTo(40));
        Assert.That(biased[1].Weight, Is.EqualTo(30));
        Assert.That(biased[2].Weight, Is.EqualTo(40));
        Assert.That(biased[3].Weight, Is.EqualTo(30));
    }

    [Test]
    public void WeightedTable_DoesNotMutateDefault()
    {
        // Sanity guard against accidental in-place mutation
        var snapshot = new int[ForageLogic.DefaultTable.Length];
        for (int i = 0; i < snapshot.Length; i++) snapshot[i] = ForageLogic.DefaultTable[i].Weight;

        _ = ForageLogic.WeightedTableForGrade(ForageLogic.ForageGrade.Perfect);

        for (int i = 0; i < snapshot.Length; i++)
            Assert.That(ForageLogic.DefaultTable[i].Weight, Is.EqualTo(snapshot[i]));
    }

    [Test]
    public void WeightedTable_EmptyBase_Throws()
    {
        Assert.Throws<System.ArgumentException>(
            () => ForageLogic.WeightedTableForGrade(ForageLogic.ForageGrade.Great, []));
    }

    // ── Grade labels ──────────────────────────────────────────────────────

    [Test]
    public void GradeLabel_AllValuesNonEmpty()
    {
        foreach (ForageLogic.ForageGrade g in System.Enum.GetValues<ForageLogic.ForageGrade>())
            Assert.That(ForageLogic.GradeLabel(g), Is.Not.Empty);
    }

    [Test]
    public void GradeArticle_AllValuesNonEmpty()
    {
        foreach (ForageLogic.ForageGrade g in System.Enum.GetValues<ForageLogic.ForageGrade>())
            Assert.That(ForageLogic.GradeArticle(g), Is.Not.Empty);
    }
}
