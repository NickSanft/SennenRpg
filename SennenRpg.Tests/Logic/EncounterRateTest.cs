using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class EncounterRateTest
{
    // ── Base-rate sanity ──────────────────────────────────────────────

    [Test]
    public void BaseRate_NoLuck_Day_Is10Percent()
    {
        float rate = EncounterLogic.EncounterRate(luck: 0, isNight: false);
        Assert.That(rate, Is.EqualTo(0.10f).Within(0.0001f));
    }

    [Test]
    public void BaseRate_NoLuck_Night_Is16Percent()
    {
        float rate = EncounterLogic.EncounterRate(luck: 0, isNight: true);
        Assert.That(rate, Is.EqualTo(0.16f).Within(0.0001f));
    }

    // ── Luck reduction ───────────────────────────────────────────────

    [Test]
    public void Luck10_Day_ReducesRateCorrectly()
    {
        // luckFactor = max(0.15, 1 - 10*0.04) = max(0.15, 0.60) = 0.60
        // rate = 0.10 * 0.60 = 0.060
        float rate = EncounterLogic.EncounterRate(luck: 10, isNight: false);
        Assert.That(rate, Is.EqualTo(0.060f).Within(0.0001f));
    }

    [Test]
    public void Luck10_Night_ReducesAndMultiplies()
    {
        // rate = 0.10 * 0.60 * 1.60 = 0.096
        float rate = EncounterLogic.EncounterRate(luck: 10, isNight: true);
        Assert.That(rate, Is.EqualTo(0.096f).Within(0.0001f));
    }

    [Test]
    public void HighLuck_CappedAtMinFactor()
    {
        // luckFactor = max(0.15, 1 - 25*0.04) = max(0.15, 0.00) = 0.15
        // rate = 0.10 * 0.15 = 0.015
        float rate = EncounterLogic.EncounterRate(luck: 25, isNight: false);
        Assert.That(rate, Is.EqualTo(0.015f).Within(0.0001f));
    }

    [Test]
    public void ExtremelyHighLuck_StillCappedAtMinFactor()
    {
        float rate100 = EncounterLogic.EncounterRate(luck: 100, isNight: false);
        float rateMin = EncounterLogic.BaseRate * EncounterLogic.MinLuckFactor;
        Assert.That(rate100, Is.EqualTo(rateMin).Within(0.0001f));
    }

    // ── Night always beats day at same luck ──────────────────────────

    [Test]
    public void Night_AlwaysHigherThanDay_AtSameLuck()
    {
        for (int luck = 0; luck <= 20; luck += 5)
        {
            float day   = EncounterLogic.EncounterRate(luck, isNight: false);
            float night = EncounterLogic.EncounterRate(luck, isNight: true);
            Assert.That(night, Is.GreaterThan(day),
                $"Night rate should exceed day rate at luck={luck}");
        }
    }

    // ── Higher luck → lower rate (same time of day) ──────────────────

    [Test]
    public void HigherLuck_LowerRate_SameTimeOfDay()
    {
        float lowLuck  = EncounterLogic.EncounterRate(luck: 0,  isNight: false);
        float highLuck = EncounterLogic.EncounterRate(luck: 10, isNight: false);
        Assert.That(highLuck, Is.LessThan(lowLuck));
    }

    // ── Parameterised accuracy ────────────────────────────────────────

    [TestCase(0,  false, 0.1000f)]
    [TestCase(10, false, 0.0600f)]
    [TestCase(0,  true,  0.1600f)]
    [TestCase(10, true,  0.0960f)]
    [TestCase(25, false, 0.0150f)]
    [TestCase(25, true,  0.0240f)]
    public void EncounterRate_Parameterized(int luck, bool night, float expected)
    {
        float rate = EncounterLogic.EncounterRate(luck, night);
        Assert.That(rate, Is.EqualTo(expected).Within(0.0001f));
    }

    // ── Rate always between 0 and 1 ───────────────────────────────────

    [Test]
    public void Rate_IsAlwaysBetweenZeroAndOne()
    {
        foreach (int luck in new[] { 0, 5, 10, 25, 100 })
        {
            foreach (bool night in new[] { false, true })
            {
                float rate = EncounterLogic.EncounterRate(luck, night);
                Assert.That(rate, Is.InRange(0f, 1f),
                    $"Rate out of [0,1] at luck={luck}, night={night}");
            }
        }
    }
}
