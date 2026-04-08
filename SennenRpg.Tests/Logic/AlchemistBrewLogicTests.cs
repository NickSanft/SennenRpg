using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class AlchemistBrewLogicTests
{
    // ── Sweet half-width scaling ──────────────────────────────────────

    [Test]
    public void SweetHalfWidth_AtLuckZero_IsBaseTenPercent()
    {
        Assert.That(AlchemistBrewLogic.SweetHalfWidth(0), Is.EqualTo(0.10f).Within(0.0001f));
    }

    [Test]
    public void SweetHalfWidth_GrowsWithLuck()
    {
        float at0  = AlchemistBrewLogic.SweetHalfWidth(0);
        float at20 = AlchemistBrewLogic.SweetHalfWidth(20);
        float at40 = AlchemistBrewLogic.SweetHalfWidth(40);

        Assert.That(at20, Is.GreaterThan(at0));
        Assert.That(at40, Is.GreaterThan(at20));
        Assert.That(at20, Is.EqualTo(0.20f).Within(0.0001f)); // 0.10 + 20*0.005 = 0.20
        Assert.That(at40, Is.EqualTo(0.30f).Within(0.0001f)); // 0.10 + 40*0.005 = 0.30
    }

    [Test]
    public void SweetHalfWidth_CapsAtForty()
    {
        Assert.That(AlchemistBrewLogic.SweetHalfWidth(60),  Is.EqualTo(0.40f).Within(0.0001f));
        Assert.That(AlchemistBrewLogic.SweetHalfWidth(999), Is.EqualTo(0.40f).Within(0.0001f));
    }

    [Test]
    public void SweetHalfWidth_NegativeLuck_TreatedAsZero()
    {
        Assert.That(AlchemistBrewLogic.SweetHalfWidth(-5), Is.EqualTo(0.10f).Within(0.0001f));
    }

    // ── Resolve outcomes ──────────────────────────────────────────────

    [Test]
    public void Resolve_DeadCenter_HighRoll_IsShield()
    {
        // accuracy=1.0 always falls in sweet spot. roll=0.95 lands in ShieldSelf range (>= 0.80).
        var result = AlchemistBrewLogic.Resolve(accuracy: 1.0f, luck: 0, roll: 0.95f);
        Assert.That(result, Is.EqualTo(BrewResult.ShieldSelf));
    }

    [Test]
    public void Resolve_DeadCenter_LowRoll_IsHeal()
    {
        var result = AlchemistBrewLogic.Resolve(accuracy: 1.0f, luck: 0, roll: 0.10f);
        Assert.That(result, Is.EqualTo(BrewResult.Heal));
    }

    [Test]
    public void Resolve_DeadCenter_MidRoll_IsPoison()
    {
        var result = AlchemistBrewLogic.Resolve(accuracy: 1.0f, luck: 0, roll: 0.60f);
        Assert.That(result, Is.EqualTo(BrewResult.PoisonEnemy));
    }

    [Test]
    public void Resolve_NarrowMiss_IsNeutral()
    {
        // accuracy=0.50 is far from center. With luck=0, sweet spot covers accuracy >= 0.80.
        // 0.50 falls into the neutral band (0.30..sweet).
        var result = AlchemistBrewLogic.Resolve(accuracy: 0.50f, luck: 0, roll: 0.50f);
        Assert.That(result, Is.EqualTo(BrewResult.Neutral));
    }

    [Test]
    public void Resolve_WideMiss_Backfires()
    {
        // accuracy=0.10 is below the 0.30 neutral floor → Backfire.
        var result = AlchemistBrewLogic.Resolve(accuracy: 0.10f, luck: 0, roll: 0.50f);
        Assert.That(result, Is.EqualTo(BrewResult.Backfire));
    }

    [Test]
    public void Resolve_HighLuck_WidensSweetSpot()
    {
        // At luck=60, sweet half-width caps at 0.40, so sweet spot covers accuracy >= 1 - 0.80 = 0.20.
        // accuracy 0.50 was Neutral at luck=0; should now be a sweet brew.
        var result = AlchemistBrewLogic.Resolve(accuracy: 0.50f, luck: 60, roll: 0.10f);
        Assert.That(result, Is.EqualTo(BrewResult.Heal));
    }

    [Test]
    public void Resolve_OutOfRangeAccuracy_Clamped()
    {
        Assert.That(AlchemistBrewLogic.Resolve(-1.0f, 0, 0.50f), Is.EqualTo(BrewResult.Backfire));
        Assert.That(AlchemistBrewLogic.Resolve( 2.0f, 0, 0.10f), Is.EqualTo(BrewResult.Heal));
    }

    // ── Damage / heal scaling ─────────────────────────────────────────

    [Test]
    public void HealAmount_ScalesWithMagic_FloorAtEight()
    {
        Assert.That(AlchemistBrewLogic.HealAmount(0),  Is.EqualTo(10));
        Assert.That(AlchemistBrewLogic.HealAmount(5),  Is.EqualTo(20));
        Assert.That(AlchemistBrewLogic.HealAmount(20), Is.EqualTo(50));
    }

    [Test]
    public void BackfireDamage_FloorAtTwo()
    {
        Assert.That(AlchemistBrewLogic.BackfireDamage(0),  Is.EqualTo(3));
        Assert.That(AlchemistBrewLogic.BackfireDamage(10), Is.EqualTo(8));
    }
}
