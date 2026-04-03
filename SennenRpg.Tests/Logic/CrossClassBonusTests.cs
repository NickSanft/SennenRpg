using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Linq;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class CrossClassBonusTests
{
    [Test]
    public void Registry_HasEntries()
    {
        Assert.That(CrossClassBonusRegistry.All, Is.Not.Empty);
    }

    [Test]
    public void Registry_NoDuplicateClassLevelPairs()
    {
        var pairs = CrossClassBonusRegistry.All
            .Select(b => (b.SourceClass, b.RequiredLevel))
            .ToList();

        Assert.That(pairs.Distinct().Count(), Is.EqualTo(pairs.Count),
            "Duplicate (SourceClass, RequiredLevel) pair found in registry");
    }

    [Test]
    public void Registry_AllRequiredLevelsPositive()
    {
        Assert.That(CrossClassBonusRegistry.All,
            Has.All.Matches<CrossClassBonus>(b => b.RequiredLevel > 0));
    }

    [Test]
    public void Registry_AllDescriptionsNonEmpty()
    {
        Assert.That(CrossClassBonusRegistry.All,
            Has.All.Matches<CrossClassBonus>(b => !string.IsNullOrWhiteSpace(b.Description)));
    }

    [Test]
    public void Registry_AllFourClassesRepresented()
    {
        var classes = CrossClassBonusRegistry.All
            .Select(b => b.SourceClass)
            .Distinct()
            .ToList();

        Assert.That(classes, Does.Contain(PlayerClass.Bard));
        Assert.That(classes, Does.Contain(PlayerClass.Fighter));
        Assert.That(classes, Does.Contain(PlayerClass.Ranger));
        Assert.That(classes, Does.Contain(PlayerClass.Mage));
    }

    [Test]
    public void Registry_EachBonusHasStatOrSpell()
    {
        foreach (var bonus in CrossClassBonusRegistry.All)
        {
            bool hasStat = bonus.StatBonuses != default;
            bool hasSpell = bonus.UnlockedSpellPath != null;
            Assert.That(hasStat || hasSpell, Is.True,
                $"Bonus '{bonus.Description}' has neither stat bonuses nor a spell unlock");
        }
    }
}
