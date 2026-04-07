using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class WeatherLogicTests
{
    // ── TransitionTable validation ────────────────────────────────────────

    [Test]
    public void DefaultTable_RowsSumToOne()
    {
        Assert.That(WeatherTransitionTable.Default.Validate(), Is.True);
    }

    [Test]
    public void Table_InvalidRow_FailsValidation()
    {
        var bad = new WeatherTransitionTable(new double[,]
        {
            { 0.5, 0.5, 0.0, 0.0, 0.0 },
            { 1.0, 0.0, 0.0, 0.0, 0.0 },
            { 1.0, 0.0, 0.0, 0.0, 0.0 },
            { 1.0, 0.0, 0.0, 0.0, 0.0 },
            { 1.0, 0.0, 0.1, 0.0, 0.0 }, // sums to 1.1
        });
        Assert.That(bad.Validate(), Is.False);
    }

    [Test]
    public void Table_WrongDimensions_Throws()
    {
        Assert.Throws<System.ArgumentException>(
            () => new WeatherTransitionTable(new double[4, 4]));
    }

    // ── ShouldRoll ────────────────────────────────────────────────────────

    [Test]
    public void ShouldRoll_ZeroCounter_ReturnsFalse()
    {
        Assert.That(WeatherLogic.ShouldRoll(0, 80), Is.False);
    }

    [TestCase(79, 80, false)]
    [TestCase(80, 80, true)]
    [TestCase(81, 80, false)]
    [TestCase(160, 80, true)]
    [TestCase(240, 80, true)]
    public void ShouldRoll_Boundaries(int counter, int interval, bool expected)
    {
        Assert.That(WeatherLogic.ShouldRoll(counter, interval), Is.EqualTo(expected));
    }

    [Test]
    public void ShouldRoll_ZeroInterval_ReturnsFalse()
    {
        Assert.That(WeatherLogic.ShouldRoll(80, 0), Is.False);
    }

    // ── RollNext ──────────────────────────────────────────────────────────

    [Test]
    public void RollNext_LowRoll_ReturnsFirstBucket()
    {
        // Sunny row starts with 0.54 Sunny → low roll must stay Sunny.
        var next = WeatherLogic.RollNext(WeatherType.Sunny, 0.0, WeatherTransitionTable.Default);
        Assert.That(next, Is.EqualTo(WeatherType.Sunny));
    }

    [Test]
    public void RollNext_Sunny_RollInAuroraBucket_ReturnsAurora()
    {
        // Sunny row cumulative: 0.54 + 0.20 + 0.20 + 0.05 = 0.99, Aurora is [0.99, 1.00)
        var next = WeatherLogic.RollNext(WeatherType.Sunny, 0.995, WeatherTransitionTable.Default);
        Assert.That(next, Is.EqualTo(WeatherType.Aurora));
    }

    [Test]
    public void RollNext_Aurora_AlwaysReturnsSunny()
    {
        // Aurora row is [1, 0, 0, 0, 0] — regardless of roll, next state is Sunny.
        Assert.That(
            WeatherLogic.RollNext(WeatherType.Aurora, 0.0,  WeatherTransitionTable.Default),
            Is.EqualTo(WeatherType.Sunny));
        Assert.That(
            WeatherLogic.RollNext(WeatherType.Aurora, 0.99, WeatherTransitionTable.Default),
            Is.EqualTo(WeatherType.Sunny));
    }

    [Test]
    public void RollNext_Snowy_StickySelfBucketIsLargest()
    {
        // Snowy row: self weight 0.40. Cumulative through Stormy is 0.60, so the
        // Snowy bucket is [0.60, 1.00). Roll above 0.60 should always return Snowy.
        Assert.That(
            WeatherLogic.RollNext(WeatherType.Snowy, 0.65, WeatherTransitionTable.Default),
            Is.EqualTo(WeatherType.Snowy));
        Assert.That(
            WeatherLogic.RollNext(WeatherType.Snowy, 0.99, WeatherTransitionTable.Default),
            Is.EqualTo(WeatherType.Snowy));
    }

    [Test]
    public void RollNext_NoAuroraFlag_AuroraAvailableFromStart()
    {
        // Regression: the plan explicitly says Aurora is available from the very first
        // roll with no story-flag gating. This test would fail if Aurora were gated
        // behind an extra parameter.
        var table = WeatherTransitionTable.Default;
        Assert.That(table.Prob(WeatherType.Sunny, WeatherType.Aurora), Is.GreaterThan(0.0));
    }

    [Test]
    public void RollNext_NullTable_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => WeatherLogic.RollNext(WeatherType.Sunny, 0.5, null!));
    }

    // ── BgmPathFor ────────────────────────────────────────────────────────

    [Test]
    public void BgmPathFor_Sunny_ReturnsSuppliedPath()
    {
        string sunny = "res://assets/music/CustomMapBgm.wav";
        Assert.That(WeatherLogic.BgmPathFor(WeatherType.Sunny, sunny), Is.EqualTo(sunny));
    }

    [Test]
    public void BgmPathFor_Foggy_ReturnsFoggyTrack()
    {
        Assert.That(
            WeatherLogic.BgmPathFor(WeatherType.Foggy, "unused"),
            Does.EndWith("Foggy Morning in Flas.wav"));
    }

    [Test]
    public void BgmPathFor_Stormy_ReturnsRainTrack()
    {
        Assert.That(
            WeatherLogic.BgmPathFor(WeatherType.Stormy, "unused"),
            Does.EndWith("A Calm Rain in Argyre.wav"));
    }

    [Test]
    public void BgmPathFor_Snowy_ReturnsGlimmersong()
    {
        Assert.That(
            WeatherLogic.BgmPathFor(WeatherType.Snowy, "unused"),
            Does.EndWith("Glimmersong Forest.wav"));
    }

    [Test]
    public void BgmPathFor_Aurora_ReturnsOriginsTrack()
    {
        Assert.That(
            WeatherLogic.BgmPathFor(WeatherType.Aurora, "unused"),
            Does.EndWith("Origins Of The Gyre.wav"));
    }

    // ── DisplayName ───────────────────────────────────────────────────────

    [TestCase(WeatherType.Sunny)]
    [TestCase(WeatherType.Foggy)]
    [TestCase(WeatherType.Stormy)]
    [TestCase(WeatherType.Snowy)]
    [TestCase(WeatherType.Aurora)]
    public void DisplayName_AllValuesNonEmpty(WeatherType w)
    {
        Assert.That(WeatherLogic.DisplayName(w), Is.Not.Empty);
    }

    // ── ApplyWeatherBias ──────────────────────────────────────────────────

    private static ForageTableEntry[] Base()
        => new[]
        {
            new ForageTableEntry("slime", 40),
            new ForageTableEntry("hair",  30),
            new ForageTableEntry("shard", 20),
            new ForageTableEntry("flower", 10),
        };

    [Test]
    public void ApplyWeatherBias_Sunny_NoChange()
    {
        var biased = WeatherLogic.ApplyWeatherBias(Base(), WeatherType.Sunny);
        Assert.That(biased[0].Weight, Is.EqualTo(40));
        Assert.That(biased[3].Weight, Is.EqualTo(10));
    }

    [Test]
    public void ApplyWeatherBias_Stormy_DoublesSlimeWeight()
    {
        var biased = WeatherLogic.ApplyWeatherBias(Base(), WeatherType.Stormy);
        Assert.That(biased[0].Weight, Is.EqualTo(80));
        Assert.That(biased[1].Weight, Is.EqualTo(30));
    }

    [Test]
    public void ApplyWeatherBias_Foggy_DoublesRareHalf()
    {
        var biased = WeatherLogic.ApplyWeatherBias(Base(), WeatherType.Foggy);
        Assert.That(biased[0].Weight, Is.EqualTo(40));
        Assert.That(biased[1].Weight, Is.EqualTo(30));
        Assert.That(biased[2].Weight, Is.EqualTo(40));
        Assert.That(biased[3].Weight, Is.EqualTo(20));
    }

    [Test]
    public void ApplyWeatherBias_Snowy_HalvesCommonHalf_BoostsRarest()
    {
        var biased = WeatherLogic.ApplyWeatherBias(Base(), WeatherType.Snowy);
        Assert.That(biased[0].Weight, Is.EqualTo(20));
        Assert.That(biased[1].Weight, Is.EqualTo(15));
        Assert.That(biased[2].Weight, Is.EqualTo(20));
        Assert.That(biased[3].Weight, Is.EqualTo(15));
    }

    [Test]
    public void ApplyWeatherBias_Aurora_TriplesRareHalfAndStacksRarestBoost()
    {
        var biased = WeatherLogic.ApplyWeatherBias(Base(), WeatherType.Aurora);
        // Rare half ×3 then rarest ×2 on top: [40, 30, 60, 10*3*2=60]
        Assert.That(biased[0].Weight, Is.EqualTo(40));
        Assert.That(biased[1].Weight, Is.EqualTo(30));
        Assert.That(biased[2].Weight, Is.EqualTo(60));
        Assert.That(biased[3].Weight, Is.EqualTo(60));
    }

    [Test]
    public void ApplyWeatherBias_DoesNotMutateInput()
    {
        var src = Base();
        _ = WeatherLogic.ApplyWeatherBias(src, WeatherType.Aurora);
        Assert.That(src[3].Weight, Is.EqualTo(10));
    }

    [Test]
    public void ApplyWeatherBias_EmptyTable_ReturnsEmpty()
    {
        var biased = WeatherLogic.ApplyWeatherBias([], WeatherType.Stormy);
        Assert.That(biased, Is.Empty);
    }

    [Test]
    public void ApplyWeatherBias_NullTable_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => WeatherLogic.ApplyWeatherBias(null!, WeatherType.Stormy));
    }

    // ── StatBuffMultiplier ────────────────────────────────────────────────

    [Test]
    public void StatBuffMultiplier_AuroraIsFivePercent()
    {
        Assert.That(WeatherLogic.StatBuffMultiplier(WeatherType.Aurora), Is.EqualTo(1.05f));
    }

    [Test]
    public void StatBuffMultiplier_AllOthersAreOne()
    {
        Assert.That(WeatherLogic.StatBuffMultiplier(WeatherType.Sunny),  Is.EqualTo(1.00f));
        Assert.That(WeatherLogic.StatBuffMultiplier(WeatherType.Foggy),  Is.EqualTo(1.00f));
        Assert.That(WeatherLogic.StatBuffMultiplier(WeatherType.Stormy), Is.EqualTo(1.00f));
        Assert.That(WeatherLogic.StatBuffMultiplier(WeatherType.Snowy),  Is.EqualTo(1.00f));
    }

    // ── ShouldStrikeLightning ─────────────────────────────────────────────

    [Test]
    public void Lightning_RollBelowHalfPercent_Fires()
    {
        Assert.That(WeatherLogic.ShouldStrikeLightning(0.1), Is.True);
        Assert.That(WeatherLogic.ShouldStrikeLightning(0.49), Is.True);
    }

    [Test]
    public void Lightning_RollAtOrAboveHalfPercent_DoesNotFire()
    {
        Assert.That(WeatherLogic.ShouldStrikeLightning(0.5), Is.False);
        Assert.That(WeatherLogic.ShouldStrikeLightning(1.0), Is.False);
        Assert.That(WeatherLogic.ShouldStrikeLightning(99.9), Is.False);
    }
}
