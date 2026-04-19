using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class RhythmWeatherLogicTests
{
    // -- ExtraBeatsUntilArrival --

    [TestCase(WeatherType.Sunny,  0)]
    [TestCase(WeatherType.Foggy,  0)]
    [TestCase(WeatherType.Stormy, 0)]
    [TestCase(WeatherType.Snowy,  1)]
    [TestCase(WeatherType.Aurora, 0)]
    public void ExtraBeatsUntilArrival_ReturnsExpected(WeatherType w, int expected)
        => Assert.That(RhythmWeatherLogic.ExtraBeatsUntilArrival(w), Is.EqualTo(expected));

    [Test]
    public void ExtraBeats_NonNegativeForAll()
    {
        foreach (WeatherType w in System.Enum.GetValues<WeatherType>())
            Assert.That(RhythmWeatherLogic.ExtraBeatsUntilArrival(w), Is.GreaterThanOrEqualTo(0));
    }

    // -- NoteYWobbleAmplitude --

    [TestCase(WeatherType.Sunny,  0f)]
    [TestCase(WeatherType.Foggy,  0f)]
    [TestCase(WeatherType.Stormy, 3f)]
    [TestCase(WeatherType.Snowy,  0f)]
    [TestCase(WeatherType.Aurora, 0f)]
    public void NoteYWobbleAmplitude_ReturnsExpected(WeatherType w, float expected)
        => Assert.That(RhythmWeatherLogic.NoteYWobbleAmplitude(w), Is.EqualTo(expected));

    [Test]
    public void Amplitude_NonNegativeForAll()
    {
        foreach (WeatherType w in System.Enum.GetValues<WeatherType>())
            Assert.That(RhythmWeatherLogic.NoteYWobbleAmplitude(w), Is.GreaterThanOrEqualTo(0f));
    }

    // -- NoteSpawnOpacity --

    [TestCase(WeatherType.Sunny,  1.0f)]
    [TestCase(WeatherType.Foggy,  0.0f)]
    [TestCase(WeatherType.Stormy, 1.0f)]
    [TestCase(WeatherType.Snowy,  1.0f)]
    [TestCase(WeatherType.Aurora, 1.0f)]
    public void NoteSpawnOpacity_ReturnsExpected(WeatherType w, float expected)
        => Assert.That(RhythmWeatherLogic.NoteSpawnOpacity(w), Is.EqualTo(expected));

    [Test]
    public void Opacity_InZeroOneForAll()
    {
        foreach (WeatherType w in System.Enum.GetValues<WeatherType>())
        {
            float opacity = RhythmWeatherLogic.NoteSpawnOpacity(w);
            Assert.That(opacity, Is.InRange(0f, 1f));
        }
    }

    // -- UseRainbowShift --

    [TestCase(WeatherType.Sunny,  false)]
    [TestCase(WeatherType.Foggy,  false)]
    [TestCase(WeatherType.Stormy, false)]
    [TestCase(WeatherType.Snowy,  false)]
    [TestCase(WeatherType.Aurora, true)]
    public void UseRainbowShift_ReturnsExpected(WeatherType w, bool expected)
        => Assert.That(RhythmWeatherLogic.UseRainbowShift(w), Is.EqualTo(expected));

    // -- CanLightningFlash --

    [TestCase(WeatherType.Sunny,  false)]
    [TestCase(WeatherType.Foggy,  false)]
    [TestCase(WeatherType.Stormy, true)]
    [TestCase(WeatherType.Snowy,  false)]
    [TestCase(WeatherType.Aurora, false)]
    public void CanLightningFlash_ReturnsExpected(WeatherType w, bool expected)
        => Assert.That(RhythmWeatherLogic.CanLightningFlash(w), Is.EqualTo(expected));
}
