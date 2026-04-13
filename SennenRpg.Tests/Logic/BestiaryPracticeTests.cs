using NUnit.Framework;
using SennenRpg.Core.Data;
using static SennenRpg.Core.Data.BestiaryPracticeLogic;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class BestiaryPracticeTests
{
    // --- CanPractice ---

    [Test]
    public void CanPractice_ZeroKills_ReturnsFalse()
    {
        Assert.That(BestiaryPracticeLogic.CanPractice(0), Is.False);
    }

    [Test]
    public void CanPractice_OneKill_ReturnsTrue()
    {
        Assert.That(BestiaryPracticeLogic.CanPractice(1), Is.True);
    }

    [Test]
    public void CanPractice_ManyKills_ReturnsTrue()
    {
        Assert.That(BestiaryPracticeLogic.CanPractice(99), Is.True);
    }

    // --- GradeRun ---

    [Test]
    public void GradeRun_AllPerfect_ReturnsS()
    {
        Assert.That(GradeRun(10, 0, 0), Is.EqualTo(PracticeRank.S));
    }

    [Test]
    public void GradeRun_95PercentPerfect_ReturnsS()
    {
        // 19 perfects, 1 good = 95% perfect rate
        Assert.That(GradeRun(19, 1, 0), Is.EqualTo(PracticeRank.S));
    }

    [Test]
    public void GradeRun_80PercentPerfect_ReturnsA()
    {
        // 8 perfects, 2 goods = 80% perfect rate
        Assert.That(GradeRun(8, 2, 0), Is.EqualTo(PracticeRank.A));
    }

    [Test]
    public void GradeRun_60PercentPerfect_ReturnsB()
    {
        // 6 perfects, 4 goods = 60% perfect rate
        Assert.That(GradeRun(6, 4, 0), Is.EqualTo(PracticeRank.B));
    }

    [Test]
    public void GradeRun_40PercentPerfect_ReturnsC()
    {
        // 4 perfects, 6 goods = 40% perfect rate
        Assert.That(GradeRun(4, 6, 0), Is.EqualTo(PracticeRank.C));
    }

    [Test]
    public void GradeRun_Below40Percent_ReturnsD()
    {
        // 3 perfects, 7 goods = 30% perfect rate
        Assert.That(GradeRun(3, 7, 0), Is.EqualTo(PracticeRank.D));
    }

    [Test]
    public void GradeRun_AllMiss_ReturnsD()
    {
        Assert.That(GradeRun(0, 0, 10), Is.EqualTo(PracticeRank.D));
    }

    [Test]
    public void GradeRun_AllGood_ReturnsD()
    {
        // 0% perfect rate = D
        Assert.That(GradeRun(0, 10, 0), Is.EqualTo(PracticeRank.D));
    }

    [Test]
    public void GradeRun_ZeroNotes_ReturnsS()
    {
        Assert.That(GradeRun(0, 0, 0), Is.EqualTo(PracticeRank.S));
    }

    [Test]
    public void GradeRun_BoundaryAt95_ExactlyS()
    {
        // Exactly 95%: 95 perfects out of 100 total
        Assert.That(GradeRun(95, 5, 0), Is.EqualTo(PracticeRank.S));
    }

    // --- Accuracy ---

    [Test]
    public void Accuracy_AllHit_Returns100()
    {
        Assert.That(BestiaryPracticeLogic.Accuracy(5, 5, 0), Is.EqualTo(100f));
    }

    [Test]
    public void Accuracy_AllMiss_Returns0()
    {
        Assert.That(BestiaryPracticeLogic.Accuracy(0, 0, 10), Is.EqualTo(0f));
    }

    [Test]
    public void Accuracy_HalfHit_Returns50()
    {
        Assert.That(BestiaryPracticeLogic.Accuracy(3, 2, 5), Is.EqualTo(50f));
    }

    // --- PerfectRate ---

    [Test]
    public void PerfectRate_AllPerfect_Returns100()
    {
        Assert.That(BestiaryPracticeLogic.PerfectRate(10, 0, 0), Is.EqualTo(100f));
    }

    [Test]
    public void PerfectRate_NoPerfects_Returns0()
    {
        Assert.That(BestiaryPracticeLogic.PerfectRate(0, 5, 5), Is.EqualTo(0f));
    }
}
