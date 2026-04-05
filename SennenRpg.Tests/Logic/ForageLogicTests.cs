using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class ForageLogicTests
{
    // ── ShouldForage ──────────────────────────────────────────────────

    [Test]
    public void ShouldForage_RollBelowChance_ReturnsTrue()
    {
        Assert.That(ForageLogic.ShouldForage(3.0, 5.0), Is.True);
    }

    [Test]
    public void ShouldForage_RollAboveChance_ReturnsFalse()
    {
        Assert.That(ForageLogic.ShouldForage(6.0, 5.0), Is.False);
    }

    [Test]
    public void ShouldForage_RollExactlyAtChance_ReturnsFalse()
    {
        // Strict less-than: roll == chancePercent should NOT trigger
        Assert.That(ForageLogic.ShouldForage(5.0, 5.0), Is.False);
    }

    [Test]
    public void ShouldForage_ZeroChance_AlwaysFalse()
    {
        Assert.That(ForageLogic.ShouldForage(0.0, 0.0), Is.False);
    }

    [Test]
    public void ShouldForage_RollZero_DefaultChance_ReturnsTrue()
    {
        Assert.That(ForageLogic.ShouldForage(0.0), Is.True);
    }

    // ── SelectForageItem ──────────────────────────────────────────────

    private static readonly ForageTableEntry[] TestTable =
    [
        new("item_a", 40),
        new("item_b", 30),
        new("item_c", 20),
        new("item_d", 10),
    ];

    [Test]
    public void SelectForageItem_LowRoll_ReturnsFirstItem()
    {
        string result = ForageLogic.SelectForageItem(0.0, TestTable);
        Assert.That(result, Is.EqualTo("item_a"));
    }

    [Test]
    public void SelectForageItem_HighRoll_ReturnsLastItem()
    {
        string result = ForageLogic.SelectForageItem(0.99, TestTable);
        Assert.That(result, Is.EqualTo("item_d"));
    }

    [Test]
    public void SelectForageItem_BoundaryBetweenFirstAndSecond()
    {
        // First item has weight 40/100 = 0.40 cumulative
        // Roll just below boundary → first item
        Assert.That(ForageLogic.SelectForageItem(0.39, TestTable), Is.EqualTo("item_a"));
        // Roll at boundary → second item (strict less-than)
        Assert.That(ForageLogic.SelectForageItem(0.40, TestTable), Is.EqualTo("item_b"));
    }

    [Test]
    public void SelectForageItem_BoundaryBetweenSecondAndThird()
    {
        // Cumulative: 0.40 + 0.30 = 0.70
        Assert.That(ForageLogic.SelectForageItem(0.69, TestTable), Is.EqualTo("item_b"));
        Assert.That(ForageLogic.SelectForageItem(0.70, TestTable), Is.EqualTo("item_c"));
    }

    [Test]
    public void SelectForageItem_SingleEntryTable_AlwaysReturnsThatItem()
    {
        var singleTable = new[] { new ForageTableEntry("only_item", 1) };
        Assert.That(ForageLogic.SelectForageItem(0.0, singleTable), Is.EqualTo("only_item"));
        Assert.That(ForageLogic.SelectForageItem(0.5, singleTable), Is.EqualTo("only_item"));
        Assert.That(ForageLogic.SelectForageItem(0.99, singleTable), Is.EqualTo("only_item"));
    }

    [Test]
    public void SelectForageItem_EmptyTable_Throws()
    {
        Assert.Throws<System.ArgumentException>(
            () => ForageLogic.SelectForageItem(0.5, []));
    }

    [Test]
    public void DefaultTable_HasFourEntries()
    {
        Assert.That(ForageLogic.DefaultTable, Has.Length.EqualTo(4));
    }

    [Test]
    public void DefaultTable_TotalWeightIs100()
    {
        int total = 0;
        foreach (var entry in ForageLogic.DefaultTable)
            total += entry.Weight;
        Assert.That(total, Is.EqualTo(100));
    }
}
