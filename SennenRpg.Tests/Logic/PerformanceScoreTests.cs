using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public sealed class PerformanceScoreTests
{
    private PerformanceScore _score = null!;

    [SetUp]
    public void SetUp() => _score = new PerformanceScore();

    // ── Initial state ────────────────────────────────────────────────────────

    [Test]
    public void NewScore_HasZeroTotal()
        => Assert.That(_score.Total, Is.EqualTo(0));

    [Test]
    public void NewScore_GetRating_ReturnsDash()
        => Assert.That(_score.GetRating(), Is.EqualTo("—"));

    // ── Record ───────────────────────────────────────────────────────────────

    [Test]
    public void Record_Perfect_IncrementsPerfects()
    {
        _score.Record(HitGrade.Perfect);
        Assert.That(_score.Perfects, Is.EqualTo(1));
        Assert.That(_score.Total,    Is.EqualTo(1));
    }

    [Test]
    public void Record_Good_IncrementsGoods()
    {
        _score.Record(HitGrade.Good);
        Assert.That(_score.Goods, Is.EqualTo(1));
    }

    [Test]
    public void Record_Miss_IncrementsMisses()
    {
        _score.Record(HitGrade.Miss);
        Assert.That(_score.Misses, Is.EqualTo(1));
    }

    [Test]
    public void Record_Multiple_SumsCorrectly()
    {
        _score.Record(HitGrade.Perfect);
        _score.Record(HitGrade.Good);
        _score.Record(HitGrade.Miss);
        Assert.That(_score.Total,    Is.EqualTo(3));
        Assert.That(_score.Perfects, Is.EqualTo(1));
        Assert.That(_score.Goods,    Is.EqualTo(1));
        Assert.That(_score.Misses,   Is.EqualTo(1));
    }

    // ── GetRating ────────────────────────────────────────────────────────────

    [Test]
    public void GetRating_AllPerfects_IsS()
    {
        for (int i = 0; i < 5; i++) _score.Record(HitGrade.Perfect);
        Assert.That(_score.GetRating(), Is.EqualTo("S"));
    }

    [Test]
    public void GetRating_AllMisses_IsD()
    {
        for (int i = 0; i < 5; i++) _score.Record(HitGrade.Miss);
        Assert.That(_score.GetRating(), Is.EqualTo("D"));
    }

    [Test]
    public void GetRating_AllGoods_IsB()
    {
        // ratio = (0*2 + 5) / (5*2) = 5/10 = 0.50 → B
        for (int i = 0; i < 5; i++) _score.Record(HitGrade.Good);
        Assert.That(_score.GetRating(), Is.EqualTo("B"));
    }

    [Test]
    public void GetRating_MostlyPerfect_IsA()
    {
        // 4 Perfects + 1 Good → ratio = (8+1)/10 = 0.90 → A (>=0.75, <0.95)
        for (int i = 0; i < 4; i++) _score.Record(HitGrade.Perfect);
        _score.Record(HitGrade.Good);
        Assert.That(_score.GetRating(), Is.EqualTo("A"));
    }

    [Test]
    public void GetRating_HalfMisses_IsC()
    {
        // 2 Goods + 4 Misses → ratio = (0+2)/(6*2) = 2/12 = 0.167 → D
        // Let's do 1 Perfect + 3 Misses → ratio = (2+0)/(4*2) = 2/8 = 0.25 → C boundary
        _score.Record(HitGrade.Perfect);
        for (int i = 0; i < 3; i++) _score.Record(HitGrade.Miss);
        Assert.That(_score.GetRating(), Is.EqualTo("C"));
    }

    // ── GetSummaryText ───────────────────────────────────────────────────────

    [Test]
    public void GetSummaryText_ContainsRating()
    {
        _score.Record(HitGrade.Perfect);
        string text = _score.GetSummaryText();
        Assert.That(text, Does.Contain("S"));
    }

    [Test]
    public void GetSummaryText_ContainsCounts()
    {
        _score.Record(HitGrade.Perfect);
        _score.Record(HitGrade.Good);
        _score.Record(HitGrade.Miss);
        string text = _score.GetSummaryText();
        Assert.That(text, Does.Contain("1 Perfect"));
        Assert.That(text, Does.Contain("1 Good"));
        Assert.That(text, Does.Contain("1 Miss"));
    }
}
