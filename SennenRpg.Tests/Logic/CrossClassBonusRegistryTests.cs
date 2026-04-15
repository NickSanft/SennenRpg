using System.Linq;
using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public sealed class CrossClassBonusRegistryTests
{
    [Test]
    public void Registry_IsNonEmpty()
        => Assert.That(CrossClassBonusRegistry.All.Length, Is.GreaterThan(0));

    [Test]
    public void Registry_NoDuplicateSourceClassLevelPairs()
    {
        var pairs = CrossClassBonusRegistry.All
            .Select(b => (b.SourceClass, b.RequiredLevel))
            .ToList();
        Assert.That(pairs.Distinct().Count(), Is.EqualTo(pairs.Count));
    }

    [Test]
    public void Registry_AllDescriptionsNonEmpty()
    {
        foreach (var b in CrossClassBonusRegistry.All)
            Assert.That(b.Description, Is.Not.Null.And.Not.Empty,
                $"{b.SourceClass} Lv{b.RequiredLevel} has empty description");
    }

    [Test]
    public void Registry_AllRequiredLevelsAtLeastOne()
    {
        foreach (var b in CrossClassBonusRegistry.All)
            Assert.That(b.RequiredLevel, Is.GreaterThanOrEqualTo(1),
                $"{b.SourceClass} Lv{b.RequiredLevel} has invalid RequiredLevel");
    }
}
