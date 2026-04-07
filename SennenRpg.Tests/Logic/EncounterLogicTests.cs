using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class EncounterLogicTests
{
    // ── EncounterRate ─────────────────────────────────────────────────────

    [Test]
    public void EncounterRate_ZeroLuckDay_ReturnsBaseRate()
    {
        Assert.That(EncounterLogic.EncounterRate(0, false),
            Is.EqualTo(EncounterLogic.BaseRate).Within(0.0001f));
    }

    [Test]
    public void EncounterRate_Night_AppliesMultiplier()
    {
        float day   = EncounterLogic.EncounterRate(0, false);
        float night = EncounterLogic.EncounterRate(0, true);
        Assert.That(night, Is.EqualTo(day * EncounterLogic.NightMultiplier).Within(0.0001f));
    }

    [Test]
    public void EncounterRate_HighLuck_ClampsAtMinFactor()
    {
        float rate = EncounterLogic.EncounterRate(100, false);
        float minRate = EncounterLogic.BaseRate * EncounterLogic.MinLuckFactor;
        Assert.That(rate, Is.EqualTo(minRate).Within(0.0001f));
    }

    // ── WeatherWeightMultiplier ───────────────────────────────────────────

    [Test]
    public void WeatherWeight_EmptyPreferred_ReturnsOne()
    {
        Assert.That(
            EncounterLogic.WeatherWeightMultiplier((int)WeatherType.Stormy, []),
            Is.EqualTo(1f));
    }

    [Test]
    public void WeatherWeight_NullPreferred_ReturnsOne()
    {
        Assert.That(
            EncounterLogic.WeatherWeightMultiplier((int)WeatherType.Stormy, null!),
            Is.EqualTo(1f));
    }

    [Test]
    public void WeatherWeight_MatchingWeather_ReturnsTwo()
    {
        int[] preferred = [(int)WeatherType.Stormy, (int)WeatherType.Sunny];
        Assert.That(
            EncounterLogic.WeatherWeightMultiplier((int)WeatherType.Stormy, preferred),
            Is.EqualTo(2f));
    }

    [Test]
    public void WeatherWeight_NonMatchingWeather_ReturnsOne()
    {
        int[] preferred = [(int)WeatherType.Stormy];
        Assert.That(
            EncounterLogic.WeatherWeightMultiplier((int)WeatherType.Sunny, preferred),
            Is.EqualTo(1f));
    }

    [TestCase(WeatherType.Sunny)]
    [TestCase(WeatherType.Foggy)]
    [TestCase(WeatherType.Stormy)]
    [TestCase(WeatherType.Snowy)]
    [TestCase(WeatherType.Aurora)]
    public void WeatherWeight_AllWeathersHandled(WeatherType weather)
    {
        // No preference → baseline multiplier regardless of current weather.
        Assert.That(
            EncounterLogic.WeatherWeightMultiplier((int)weather, []),
            Is.EqualTo(1f));
    }
}
