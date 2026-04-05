using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class JunkSellLogicTests
{
    [Test]
    public void CountJunkItems_EmptyList_ReturnsZero()
    {
        var items = System.Array.Empty<(string, int)>();
        Assert.That(JunkSellLogic.CountJunkItems(items), Is.EqualTo(0));
    }

    [Test]
    public void CountJunkItems_ThreeItems_ReturnsThree()
    {
        (string, int)[] items =
        [
            ("junk_a", 10),
            ("junk_b", 20),
            ("junk_c", 5),
        ];
        Assert.That(JunkSellLogic.CountJunkItems(items), Is.EqualTo(3));
    }

    [Test]
    public void TotalJunkValue_EmptyList_ReturnsZero()
    {
        var items = System.Array.Empty<(string, int)>();
        Assert.That(JunkSellLogic.TotalJunkValue(items), Is.EqualTo(0));
    }

    [Test]
    public void TotalJunkValue_MixedValues_ReturnsSumOfSellValues()
    {
        (string, int)[] items =
        [
            ("junk_a", 30),
            ("junk_b", 10),
            ("junk_c", 5),
        ];
        Assert.That(JunkSellLogic.TotalJunkValue(items), Is.EqualTo(45));
    }

    [Test]
    public void TotalJunkValue_SingleItem_ReturnsItsValue()
    {
        (string, int)[] items = [("junk_a", 20)];
        Assert.That(JunkSellLogic.TotalJunkValue(items), Is.EqualTo(20));
    }
}
