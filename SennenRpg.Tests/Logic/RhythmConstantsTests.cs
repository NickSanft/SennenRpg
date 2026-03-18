using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public sealed class RhythmConstantsTests
{
    // ── BeatInterval ─────────────────────────────────────────────────────────

    [TestCase(60f,  1.000f)]
    [TestCase(120f, 0.500f)]
    [TestCase(180f, 0.3333f)]
    [TestCase(240f, 0.250f)]
    public void BeatInterval_ReturnsSecondsPerBeat(float bpm, float expected)
        => Assert.That(RhythmConstants.BeatInterval(bpm), Is.EqualTo(expected).Within(0.0001f));

    // ── GradeDeviation ───────────────────────────────────────────────────────

    [Test]
    public void GradeDeviation_ExactlyZero_IsPerfect()
        => Assert.That(RhythmConstants.GradeDeviation(0f), Is.EqualTo(HitGrade.Perfect));

    [Test]
    public void GradeDeviation_WithinPerfectWindow_IsPerfect()
        => Assert.That(RhythmConstants.GradeDeviation(RhythmConstants.PerfectWindowSec), Is.EqualTo(HitGrade.Perfect));

    [Test]
    public void GradeDeviation_NegativeWithinPerfectWindow_IsPerfect()
        => Assert.That(RhythmConstants.GradeDeviation(-RhythmConstants.PerfectWindowSec), Is.EqualTo(HitGrade.Perfect));

    [Test]
    public void GradeDeviation_BetweenWindowsPositive_IsGood()
    {
        float mid = (RhythmConstants.PerfectWindowSec + RhythmConstants.GoodWindowSec) * 0.5f + 0.001f;
        Assert.That(RhythmConstants.GradeDeviation(mid), Is.EqualTo(HitGrade.Good));
    }

    [Test]
    public void GradeDeviation_AtGoodWindowEdge_IsGood()
        => Assert.That(RhythmConstants.GradeDeviation(RhythmConstants.GoodWindowSec), Is.EqualTo(HitGrade.Good));

    [Test]
    public void GradeDeviation_BeyondGoodWindow_IsMiss()
        => Assert.That(RhythmConstants.GradeDeviation(RhythmConstants.GoodWindowSec + 0.001f), Is.EqualTo(HitGrade.Miss));

    [Test]
    public void GradeDeviation_LargeNegative_IsMiss()
        => Assert.That(RhythmConstants.GradeDeviation(-1.0f), Is.EqualTo(HitGrade.Miss));

    // ── GradeMultiplier ──────────────────────────────────────────────────────

    [TestCase(HitGrade.Perfect, 1.5f)]
    [TestCase(HitGrade.Good,    1.0f)]
    [TestCase(HitGrade.Miss,    0.5f)]
    public void GradeMultiplier_ReturnsExpectedFactor(HitGrade grade, float expected)
        => Assert.That(RhythmConstants.GradeMultiplier(grade), Is.EqualTo(expected).Within(0.0001f));
}
