using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class DayNightCycleTest
{
    // ── ShouldFlip boundary tests ─────────────────────────────────────

    [Test]
    public void ShouldFlip_AtZeroTiles_ReturnsFalse()
        => Assert.That(DayNightLogic.ShouldFlip(0), Is.False);

    [Test]
    public void ShouldFlip_At19Tiles_ReturnsFalse()
        => Assert.That(DayNightLogic.ShouldFlip(19), Is.False);

    [Test]
    public void ShouldFlip_At20Tiles_ReturnsTrue()
        => Assert.That(DayNightLogic.ShouldFlip(20), Is.True);

    [Test]
    public void ShouldFlip_At21Tiles_ReturnsFalse()
        => Assert.That(DayNightLogic.ShouldFlip(21), Is.False);

    [Test]
    public void ShouldFlip_At39Tiles_ReturnsFalse()
        => Assert.That(DayNightLogic.ShouldFlip(39), Is.False);

    [Test]
    public void ShouldFlip_At40Tiles_ReturnsTrue()
        => Assert.That(DayNightLogic.ShouldFlip(40), Is.True);

    [Test]
    public void ShouldFlip_At100Tiles_ReturnsTrue()
        => Assert.That(DayNightLogic.ShouldFlip(100), Is.True);

    // ── Parameterised multiples ───────────────────────────────────────

    [TestCase(20,  true)]
    [TestCase(40,  true)]
    [TestCase(60,  true)]
    [TestCase(1,   false)]
    [TestCase(10,  false)]
    [TestCase(19,  false)]
    [TestCase(21,  false)]
    public void ShouldFlip_Parameterized(int tiles, bool expected)
        => Assert.That(DayNightLogic.ShouldFlip(tiles), Is.EqualTo(expected));

    // ── ApplyFlip ─────────────────────────────────────────────────────

    [Test]
    public void ApplyFlip_WhenDay_ReturnsNight()
        => Assert.That(DayNightLogic.ApplyFlip(currentlyNight: false), Is.True);

    [Test]
    public void ApplyFlip_WhenNight_ReturnsDay()
        => Assert.That(DayNightLogic.ApplyFlip(currentlyNight: true), Is.False);

    // ── Full simulation ───────────────────────────────────────────────

    [Test]
    public void MultipleCycles_AlternatesCorrectly()
    {
        bool isNight = false; // start day
        int flips    = 0;

        for (int i = 1; i <= 100; i++)
        {
            if (DayNightLogic.ShouldFlip(i))
            {
                isNight = DayNightLogic.ApplyFlip(isNight);
                flips++;
            }
        }

        // 100 tiles / 20 per cycle = 5 flips
        Assert.That(flips, Is.EqualTo(5));

        // Started day → after odd number of flips, should be night
        Assert.That(isNight, Is.True);
    }

    [Test]
    public void SimulationAt40Steps_IsBackToDay()
    {
        bool isNight = false;
        for (int i = 1; i <= 40; i++)
            if (DayNightLogic.ShouldFlip(i))
                isNight = DayNightLogic.ApplyFlip(isNight);

        Assert.That(isNight, Is.False); // day → night → day
    }

    [Test]
    public void CycleLengthTiles_Is20()
        => Assert.That(DayNightLogic.CycleLengthTiles, Is.EqualTo(20));
}
