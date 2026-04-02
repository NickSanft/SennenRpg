using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class RhythmMemoryLogicTests
{
    private static PerformanceScore MakeScore(int perfects, int goods, int misses)
    {
        var score = new PerformanceScore();
        for (int i = 0; i < perfects; i++) score.Record(HitGrade.Perfect);
        for (int i = 0; i < goods; i++)    score.Record(HitGrade.Good);
        for (int i = 0; i < misses; i++)   score.Record(HitGrade.Miss);
        return score;
    }

    // ── ComputeAdaptation ─────────────────────────────────────────────

    [Test]
    public void ComputeAdaptation_NullHistory_ReturnsNone()
    {
        var result = RhythmMemoryLogic.ComputeAdaptation(null);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.None));
        Assert.That(result.ExtraMeasures, Is.EqualTo(0));
        Assert.That(result.ObstacleDensityMult, Is.EqualTo(1.0f));
        Assert.That(result.BonusGoldPercent, Is.EqualTo(0f));
    }

    [Test]
    public void ComputeAdaptation_BelowThreshold_ReturnsNone()
    {
        var history = new EnemyRhythmHistory(2, 20, 0, 0, 10);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.None));
    }

    [Test]
    public void ComputeAdaptation_ZeroTotalHits_ReturnsNone()
    {
        var history = new EnemyRhythmHistory(5, 0, 0, 0, 0);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.None));
    }

    [Test]
    public void ComputeAdaptation_LowPerfectRate_ReturnsNone()
    {
        // 20% perfect rate — below 0.30 threshold
        var history = new EnemyRhythmHistory(5, 2, 5, 3, 2);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.None));
    }

    [Test]
    public void ComputeAdaptation_WaryTier()
    {
        // 40% perfect rate — between 0.30 and 0.55
        var history = new EnemyRhythmHistory(5, 4, 3, 3, 3);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.Wary));
        Assert.That(result.ExtraMeasures, Is.EqualTo(0));
        Assert.That(result.BonusGoldPercent, Is.EqualTo(0.10f));
        Assert.That(result.BonusExpPercent, Is.EqualTo(0.10f));
        Assert.That(result.BonusLootChance, Is.EqualTo(0.05f));
    }

    [Test]
    public void ComputeAdaptation_HardenedTier()
    {
        // 60% perfect rate — between 0.55 and 0.80
        var history = new EnemyRhythmHistory(5, 6, 2, 2, 5);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.Hardened));
        Assert.That(result.ExtraMeasures, Is.EqualTo(1));
        Assert.That(result.ObstacleDensityMult, Is.EqualTo(1.25f));
        Assert.That(result.BonusGoldPercent, Is.EqualTo(0.25f));
        Assert.That(result.BonusExpPercent, Is.EqualTo(0.20f));
    }

    [Test]
    public void ComputeAdaptation_RivalTier()
    {
        // 90% perfect rate — above 0.80
        var history = new EnemyRhythmHistory(5, 9, 1, 0, 8);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.Rival));
        Assert.That(result.ExtraMeasures, Is.EqualTo(2));
        Assert.That(result.ObstacleDensityMult, Is.EqualTo(1.50f));
        Assert.That(result.BonusGoldPercent, Is.EqualTo(0.50f));
        Assert.That(result.BonusExpPercent, Is.EqualTo(0.40f));
        Assert.That(result.BonusLootChance, Is.EqualTo(0.30f));
    }

    [Test]
    public void ComputeAdaptation_CockyTier()
    {
        // 10% perfect, 70% miss — enemy gets cocky
        var history = new EnemyRhythmHistory(5, 1, 2, 7, 1);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.Cocky));
        Assert.That(result.ExtraMeasures, Is.EqualTo(-1));
        Assert.That(result.ObstacleDensityMult, Is.EqualTo(0.50f));
        Assert.That(result.BonusGoldPercent, Is.EqualTo(0f));
    }

    [Test]
    public void ComputeAdaptation_ExactlyAtWaryBoundary()
    {
        // Exactly 30% perfect rate
        var history = new EnemyRhythmHistory(3, 3, 4, 3, 2);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.Wary));
    }

    [Test]
    public void ComputeAdaptation_ExactlyAtRivalBoundary()
    {
        // Exactly 80% perfect rate
        var history = new EnemyRhythmHistory(3, 8, 1, 1, 6);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.Rival));
    }

    [Test]
    public void ComputeAdaptation_HighMissButAbovePerfectThreshold_NotCocky()
    {
        // 30% perfect and 60%+ miss — perfect rate >= 0.15 so NOT cocky, but Wary
        var history = new EnemyRhythmHistory(5, 3, 0, 7, 2);
        var result = RhythmMemoryLogic.ComputeAdaptation(history);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.Wary));
    }

    // ── RecordEncounter ───────────────────────────────────────────────

    [Test]
    public void RecordEncounter_NullExisting_CreatesFreshHistory()
    {
        var score = MakeScore(5, 3, 2);
        var history = RhythmMemoryLogic.RecordEncounter(null, score);

        Assert.That(history.TotalEncounters, Is.EqualTo(1));
        Assert.That(history.TotalPerfects, Is.EqualTo(5));
        Assert.That(history.TotalGoods, Is.EqualTo(3));
        Assert.That(history.TotalMisses, Is.EqualTo(2));
        Assert.That(history.BestMaxStreak, Is.EqualTo(8)); // 5 perfects + 3 goods = streak 8
    }

    [Test]
    public void RecordEncounter_AccumulatesCorrectly()
    {
        var existing = new EnemyRhythmHistory(2, 10, 5, 3, 7);
        var score = MakeScore(4, 2, 1);
        var history = RhythmMemoryLogic.RecordEncounter(existing, score);

        Assert.That(history.TotalEncounters, Is.EqualTo(3));
        Assert.That(history.TotalPerfects, Is.EqualTo(14));
        Assert.That(history.TotalGoods, Is.EqualTo(7));
        Assert.That(history.TotalMisses, Is.EqualTo(4));
        Assert.That(history.BestMaxStreak, Is.EqualTo(7)); // existing 7 > current 4
    }

    [Test]
    public void RecordEncounter_NewStreakBeatsPrevious()
    {
        var existing = new EnemyRhythmHistory(1, 3, 2, 1, 3);
        var score = MakeScore(10, 0, 0); // streak of 10
        var history = RhythmMemoryLogic.RecordEncounter(existing, score);

        Assert.That(history.BestMaxStreak, Is.EqualTo(10));
    }

    [Test]
    public void RecordEncounter_AllMisses()
    {
        var score = MakeScore(0, 0, 5);
        var history = RhythmMemoryLogic.RecordEncounter(null, score);

        Assert.That(history.TotalPerfects, Is.EqualTo(0));
        Assert.That(history.TotalMisses, Is.EqualTo(5));
        Assert.That(history.BestMaxStreak, Is.EqualTo(0));
    }

    // ── Round-trip: Record then Compute ────────────────────────────────

    [Test]
    public void RoundTrip_ThreePerfectEncounters_BecomesRival()
    {
        EnemyRhythmHistory? h = null;
        for (int i = 0; i < 3; i++)
            h = RhythmMemoryLogic.RecordEncounter(h, MakeScore(10, 0, 0));

        var result = RhythmMemoryLogic.ComputeAdaptation(h);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.Rival));
    }

    [Test]
    public void RoundTrip_ThreeBadEncounters_BecomesCocky()
    {
        EnemyRhythmHistory? h = null;
        for (int i = 0; i < 3; i++)
            h = RhythmMemoryLogic.RecordEncounter(h, MakeScore(0, 1, 9));

        var result = RhythmMemoryLogic.ComputeAdaptation(h);
        Assert.That(result.Tier, Is.EqualTo(AdaptationTier.Cocky));
    }
}
