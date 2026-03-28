using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class PerformanceScoreTest
{
    [Test]
    public void NewScore_HasZeroEverything()
    {
        var score = new PerformanceScore();
        Assert.That(score.Total,           Is.EqualTo(0));
        Assert.That(score.CurrentStreak,   Is.EqualTo(0));
        Assert.That(score.MaxStreak,       Is.EqualTo(0));
        Assert.That(score.ComboMultiplier, Is.EqualTo(1.0f).Within(0.001f));
    }

    [Test]
    public void Record_Perfect_IncrementsStreak()
    {
        var score = new PerformanceScore();
        score.Record(HitGrade.Perfect);
        score.Record(HitGrade.Perfect);
        Assert.That(score.CurrentStreak, Is.EqualTo(2));
        Assert.That(score.MaxStreak,     Is.EqualTo(2));
    }

    [Test]
    public void Record_Good_IncrementsStreak()
    {
        var score = new PerformanceScore();
        score.Record(HitGrade.Good);
        Assert.That(score.CurrentStreak, Is.EqualTo(1));
    }

    [Test]
    public void Record_Miss_ResetsStreakPreservesMax()
    {
        var score = new PerformanceScore();
        score.Record(HitGrade.Perfect);
        score.Record(HitGrade.Perfect);
        score.Record(HitGrade.Miss);
        Assert.That(score.CurrentStreak, Is.EqualTo(0));
        Assert.That(score.MaxStreak,     Is.EqualTo(2));
    }

    [Test]
    public void ComboMultiplier_At10Streak_IsHalf()
    {
        var score = new PerformanceScore();
        for (int i = 0; i < 10; i++) score.Record(HitGrade.Perfect);
        Assert.That(score.ComboMultiplier, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void ComboMultiplier_NeverBelow50Percent()
    {
        var score = new PerformanceScore();
        for (int i = 0; i < 30; i++) score.Record(HitGrade.Perfect);
        Assert.That(score.ComboMultiplier, Is.GreaterThanOrEqualTo(0.5f));
    }

    [Test]
    public void GetRating_AllPerfects_IsS()
    {
        var score = new PerformanceScore();
        score.Record(HitGrade.Perfect);
        score.Record(HitGrade.Perfect);
        Assert.That(score.GetRating(), Is.EqualTo("S"));
    }

    [Test]
    public void GetSummaryText_IncludesMaxCombo()
    {
        var score = new PerformanceScore();
        score.Record(HitGrade.Perfect);
        score.Record(HitGrade.Perfect);
        score.Record(HitGrade.Miss);
        string summary = score.GetSummaryText();
        Assert.That(summary, Does.Contain("Max Combo: 2"));
    }
}
