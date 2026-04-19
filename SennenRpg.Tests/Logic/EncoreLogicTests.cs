using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class EncoreLogicTests
{
    // --- IsFlawless ---

    [Test]
    public void IsFlawless_AllPerfects_ReturnsTrue()
        => Assert.That(EncoreLogic.IsFlawless(5, 0, 0), Is.True);

    [Test]
    public void IsFlawless_WithGoods_ReturnsFalse()
        => Assert.That(EncoreLogic.IsFlawless(4, 1, 0), Is.False);

    [Test]
    public void IsFlawless_WithMisses_ReturnsFalse()
        => Assert.That(EncoreLogic.IsFlawless(4, 0, 1), Is.False);

    [Test]
    public void IsFlawless_ZeroNotes_ReturnsFalse()
        => Assert.That(EncoreLogic.IsFlawless(0, 0, 0), Is.False);

    // --- CanGrantEncore ---

    [Test]
    public void CanGrantEncore_FlawlessAndNotUsed_ReturnsTrue()
        => Assert.That(EncoreLogic.CanGrantEncore(true, false), Is.True);

    [Test]
    public void CanGrantEncore_NotFlawless_ReturnsFalse()
        => Assert.That(EncoreLogic.CanGrantEncore(false, false), Is.False);

    [Test]
    public void CanGrantEncore_AlreadyUsed_ReturnsFalse()
        => Assert.That(EncoreLogic.CanGrantEncore(true, true), Is.False);

    // --- ApplyEncoreBonus ---

    [Test]
    public void ApplyEncoreBonus_10_Returns15()
        => Assert.That(EncoreLogic.ApplyEncoreBonus(10), Is.EqualTo(15));

    [Test]
    public void ApplyEncoreBonus_0_Returns0()
        => Assert.That(EncoreLogic.ApplyEncoreBonus(0), Is.EqualTo(0));

    [Test]
    public void ApplyEncoreBonus_1_Returns1()
        => Assert.That(EncoreLogic.ApplyEncoreBonus(1), Is.EqualTo(1));

    [Test]
    public void ApplyEncoreBonus_7_Returns10()
        => Assert.That(EncoreLogic.ApplyEncoreBonus(7), Is.EqualTo(10));
}
