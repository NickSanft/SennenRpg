using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class ComboSpellTests
{
    // ── Registry invariants ──

    [Test]
    public void Registry_NoDuplicateIds()
    {
        var ids = ComboSpellRegistry.All.Select(s => s.Id).ToList();
        Assert.That(ids, Is.Unique);
    }

    [Test]
    public void Registry_MemberA_BeforeMemberB_Alphabetically()
    {
        foreach (var spell in ComboSpellRegistry.All)
            Assert.That(string.Compare(spell.MemberA, spell.MemberB, System.StringComparison.Ordinal),
                Is.LessThan(0), $"{spell.Id}: MemberA '{spell.MemberA}' should precede MemberB '{spell.MemberB}'");
    }

    [Test]
    public void Registry_PositiveMpCosts()
    {
        foreach (var spell in ComboSpellRegistry.All)
        {
            Assert.That(spell.MpCostA, Is.GreaterThan(0), $"{spell.Id} MpCostA");
            Assert.That(spell.MpCostB, Is.GreaterThan(0), $"{spell.Id} MpCostB");
        }
    }

    [Test]
    public void Registry_NonEmptyDisplayName()
    {
        foreach (var spell in ComboSpellRegistry.All)
            Assert.That(spell.DisplayName, Is.Not.Empty, $"{spell.Id} DisplayName");
    }

    [Test]
    public void Registry_Find_ReturnsSpell()
    {
        var result = ComboSpellRegistry.Find("Sen", "Lily");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Id, Is.EqualTo("resonance_burst"));
    }

    [Test]
    public void Registry_Find_ReversedOrder_ReturnsSpell()
    {
        var result = ComboSpellRegistry.Find("Rain", "Bhata");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Id, Is.EqualTo("gravity_volley"));
    }

    [Test]
    public void Registry_Find_UnknownPair_ReturnsNull()
    {
        Assert.That(ComboSpellRegistry.Find("Sen", "Bhata"), Is.Null);
    }

    // ── NormalizePair ──

    [Test]
    public void NormalizePair_SortsCorrectly()
    {
        var (a, b) = ComboSpellLogic.NormalizePair("Sen", "Lily");
        Assert.That(a, Is.EqualTo("Lily"));
        Assert.That(b, Is.EqualTo("Sen"));
    }

    [Test]
    public void NormalizePair_AlreadySorted_PassesThrough()
    {
        var (a, b) = ComboSpellLogic.NormalizePair("Bhata", "Rain");
        Assert.That(a, Is.EqualTo("Bhata"));
        Assert.That(b, Is.EqualTo("Rain"));
    }

    // ── FindAvailableCombo ──

    [Test]
    public void FindAvailableCombo_ReturnsNull_WhenNextIsEnemy()
    {
        var queue = new List<TurnQueueEntry>
        {
            new(IsParty: true,  Index: 0, Speed: 10),
            new(IsParty: false, Index: 0, Speed: 8),
        };
        var result = ComboSpellLogic.FindAvailableCombo(queue, 0,
            i => i == 0 ? "Sen" : "Lily",
            _ => 99);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindAvailableCombo_ReturnsNull_WhenNotAdjacentPartyMembers()
    {
        var queue = new List<TurnQueueEntry>
        {
            new(IsParty: true, Index: 0, Speed: 10),
        };
        var result = ComboSpellLogic.FindAvailableCombo(queue, 0,
            _ => "Sen",
            _ => 99);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindAvailableCombo_ReturnsNull_WhenCannotAfford()
    {
        var queue = new List<TurnQueueEntry>
        {
            new(IsParty: true, Index: 0, Speed: 10),
            new(IsParty: true, Index: 1, Speed: 9),
        };
        var result = ComboSpellLogic.FindAvailableCombo(queue, 0,
            i => i == 0 ? "Sen" : "Lily",
            _ => 2); // not enough MP
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindAvailableCombo_ReturnsSpell_WhenConditionsMet()
    {
        var queue = new List<TurnQueueEntry>
        {
            new(IsParty: true, Index: 0, Speed: 10),
            new(IsParty: true, Index: 1, Speed: 9),
        };
        var result = ComboSpellLogic.FindAvailableCombo(queue, 0,
            i => i == 0 ? "Sen" : "Lily",
            _ => 20);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Id, Is.EqualTo("resonance_burst"));
    }

    [Test]
    public void FindAvailableCombo_ReturnsNull_WhenNoPairInRegistry()
    {
        var queue = new List<TurnQueueEntry>
        {
            new(IsParty: true, Index: 0, Speed: 10),
            new(IsParty: true, Index: 1, Speed: 9),
        };
        var result = ComboSpellLogic.FindAvailableCombo(queue, 0,
            i => i == 0 ? "Sen" : "Bhata",
            _ => 99);
        Assert.That(result, Is.Null);
    }

    // ── ResolveComboSpellDamage ──

    [Test]
    public void ResolveDamage_Physical_UsesAttacks()
    {
        int dmg = ComboSpellLogic.ResolveComboSpellDamage(
            ComboSpellType.Physical,
            attackA: 20, attackB: 20, magicA: 100, magicB: 100,
            targetDef: 10, targetRes: 100);
        // (20+20)*1.5 - 10 = 50
        Assert.That(dmg, Is.EqualTo(50));
    }

    [Test]
    public void ResolveDamage_Magical_UsesMagic()
    {
        int dmg = ComboSpellLogic.ResolveComboSpellDamage(
            ComboSpellType.Magical,
            attackA: 100, attackB: 100, magicA: 20, magicB: 20,
            targetDef: 100, targetRes: 10);
        // (20+20)*1.5 - 10 = 50
        Assert.That(dmg, Is.EqualTo(50));
    }

    [Test]
    public void ResolveDamage_Hybrid_UsesAttackAndMagic()
    {
        int dmg = ComboSpellLogic.ResolveComboSpellDamage(
            ComboSpellType.Hybrid,
            attackA: 20, attackB: 0, magicA: 0, magicB: 20,
            targetDef: 10, targetRes: 10);
        // (20+20)*1.5 - (10+10)/2 = 60 - 10 = 50
        Assert.That(dmg, Is.EqualTo(50));
    }

    [Test]
    public void ResolveDamage_MinimumIsOne()
    {
        int dmg = ComboSpellLogic.ResolveComboSpellDamage(
            ComboSpellType.Physical,
            attackA: 1, attackB: 1, magicA: 1, magicB: 1,
            targetDef: 9999, targetRes: 9999);
        Assert.That(dmg, Is.EqualTo(1));
    }

    [Test]
    public void ResolveDamage_AccuracyScalesOutput()
    {
        int full = ComboSpellLogic.ResolveComboSpellDamage(
            ComboSpellType.Physical,
            attackA: 20, attackB: 20, magicA: 0, magicB: 0,
            targetDef: 0, targetRes: 0, accuracy: 1.0f);
        int half = ComboSpellLogic.ResolveComboSpellDamage(
            ComboSpellType.Physical,
            attackA: 20, attackB: 20, magicA: 0, magicB: 0,
            targetDef: 0, targetRes: 0, accuracy: 0.5f);
        // full = 60, half = 30
        Assert.That(full, Is.EqualTo(60));
        Assert.That(half, Is.EqualTo(30));
    }

    // ── CanAfford ──

    [Test]
    public void CanAfford_True_WhenBothHaveEnough()
    {
        var spell = new ComboSpell("test", "Test", "A", "B", 5, 10, ComboSpellType.Physical);
        Assert.That(ComboSpellLogic.CanAfford(spell, 5, 10), Is.True);
    }

    [Test]
    public void CanAfford_False_WhenA_IsShort()
    {
        var spell = new ComboSpell("test", "Test", "A", "B", 5, 10, ComboSpellType.Physical);
        Assert.That(ComboSpellLogic.CanAfford(spell, 4, 10), Is.False);
    }

    [Test]
    public void CanAfford_False_WhenB_IsShort()
    {
        var spell = new ComboSpell("test", "Test", "A", "B", 5, 10, ComboSpellType.Physical);
        Assert.That(ComboSpellLogic.CanAfford(spell, 5, 9), Is.False);
    }
}
