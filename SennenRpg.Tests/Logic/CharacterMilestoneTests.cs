using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public sealed class CharacterMilestoneTests
{
    private static PartyMember Make(string id, int level)
        => new() { MemberId = id, DisplayName = id, Level = level };

    // ── Registry validation ─────────────────────────────────────────

    [Test]
    public void Registry_Has20Entries()
        => Assert.That(CharacterMilestoneRegistry.All.Length, Is.EqualTo(20));

    [Test]
    public void Registry_FiveMembersWithFourEach()
    {
        var grouped = CharacterMilestoneRegistry.All.GroupBy(m => m.MemberId).ToList();
        Assert.That(grouped.Count, Is.EqualTo(5));
        foreach (var g in grouped)
            Assert.That(g.Count(), Is.EqualTo(4), $"Member {g.Key} should have 4 milestones");
    }

    [Test]
    public void Registry_NoDuplicateMemberLevelPairs()
    {
        var pairs = CharacterMilestoneRegistry.All.Select(m => (m.MemberId, m.RequiredLevel)).ToList();
        Assert.That(pairs.Distinct().Count(), Is.EqualTo(pairs.Count));
    }

    [Test]
    public void Registry_AllRequiredLevelsAreMultiplesOf5()
    {
        foreach (var m in CharacterMilestoneRegistry.All)
            Assert.That(m.RequiredLevel % 5, Is.EqualTo(0),
                $"{m.MemberId} Lv{m.RequiredLevel} is not a multiple of 5");
    }

    [Test]
    public void Registry_AllDescriptionsNonEmpty()
    {
        foreach (var m in CharacterMilestoneRegistry.All)
            Assert.That(m.Description, Is.Not.Null.And.Not.Empty,
                $"{m.MemberId} Lv{m.RequiredLevel} has empty description");
    }

    [Test]
    public void Registry_EachBonusHasStatOrTag()
    {
        foreach (var m in CharacterMilestoneRegistry.All)
        {
            bool hasStat = m.StatBonuses != default;
            bool hasTag  = !string.IsNullOrEmpty(m.Tag);
            Assert.That(hasStat || hasTag, Is.True,
                $"{m.MemberId} Lv{m.RequiredLevel} has neither stat bonus nor tag");
        }
    }

    [Test]
    public void Registry_AllFiveMembersRepresented()
    {
        var ids = CharacterMilestoneRegistry.All.Select(m => m.MemberId).Distinct().OrderBy(x => x).ToList();
        Assert.That(ids, Is.EquivalentTo(new[] { "bhata", "kriora", "lily", "rain", "sen" }));
    }

    // ── GetEarnedMilestones ─────────────────────────────────────────

    [Test]
    public void GetEarnedMilestones_Level0_ReturnsEmpty()
        => Assert.That(CharacterMilestoneLogic.GetEarnedMilestones("sen", 0), Is.Empty);

    [Test]
    public void GetEarnedMilestones_Level4_ReturnsEmpty()
        => Assert.That(CharacterMilestoneLogic.GetEarnedMilestones("sen", 4), Is.Empty);

    [Test]
    public void GetEarnedMilestones_Level5_ReturnsSingle()
    {
        var earned = CharacterMilestoneLogic.GetEarnedMilestones("sen", 5);
        Assert.That(earned.Count, Is.EqualTo(1));
        Assert.That(earned[0].RequiredLevel, Is.EqualTo(5));
    }

    [Test]
    public void GetEarnedMilestones_Level10_ReturnsTwo()
    {
        var earned = CharacterMilestoneLogic.GetEarnedMilestones("sen", 10);
        Assert.That(earned.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetEarnedMilestones_Level20_ReturnsFour()
    {
        var earned = CharacterMilestoneLogic.GetEarnedMilestones("sen", 20);
        Assert.That(earned.Count, Is.EqualTo(4));
    }

    [Test]
    public void GetEarnedMilestones_UnknownMember_ReturnsEmpty()
        => Assert.That(CharacterMilestoneLogic.GetEarnedMilestones("ghost", 20), Is.Empty);

    // ── GetMilestonesAtLevel ────────────────────────────────────────

    [Test]
    public void GetMilestonesAtLevel_ExactMatch_ReturnsMilestone()
    {
        var atLevel = CharacterMilestoneLogic.GetMilestonesAtLevel("lily", 5);
        Assert.That(atLevel.Count, Is.EqualTo(1));
        Assert.That(atLevel[0].MemberId, Is.EqualTo("lily"));
    }

    [Test]
    public void GetMilestonesAtLevel_NonThreshold_ReturnsEmpty()
        => Assert.That(CharacterMilestoneLogic.GetMilestonesAtLevel("lily", 7), Is.Empty);

    // ── SumIndividualMilestones ─────────────────────────────────────

    [Test]
    public void SumIndividualMilestones_OnlyCountsSelfNonAura()
    {
        // Sen Lv5 = +3 SPD (individual), Lv10 = +2 LCK (party-wide, excluded)
        var bonus = CharacterMilestoneLogic.SumIndividualMilestones("sen", 10);
        Assert.That(bonus.Speed, Is.EqualTo(3));
        Assert.That(bonus.Luck, Is.EqualTo(0), "Party-wide bonuses should be excluded");
    }

    [Test]
    public void SumIndividualMilestones_Level20_IncludesAllIndividual()
    {
        // Sen individual: Lv5 +3 SPD, Lv15 +5 MaxHP
        var bonus = CharacterMilestoneLogic.SumIndividualMilestones("sen", 20);
        Assert.That(bonus.Speed, Is.EqualTo(3));
        Assert.That(bonus.MaxHp, Is.EqualTo(5));
    }

    // ── SumPartyAuras ───────────────────────────────────────────────

    [Test]
    public void SumPartyAuras_SingleMemberLv10()
    {
        var members = new List<PartyMember> { Make("sen", 10) };
        var bonus = CharacterMilestoneLogic.SumPartyAuras(members);
        Assert.That(bonus.Luck, Is.EqualTo(2)); // Sen Lv10 aura
    }

    [Test]
    public void SumPartyAuras_MultipleMembersStack()
    {
        var members = new List<PartyMember>
        {
            Make("sen", 10),    // +2 LCK
            Make("lily", 10),   // +3 RES
            Make("bhata", 10),  // +2 ATK
        };
        var bonus = CharacterMilestoneLogic.SumPartyAuras(members);
        Assert.That(bonus.Luck, Is.EqualTo(2));
        Assert.That(bonus.Resistance, Is.EqualTo(3));
        Assert.That(bonus.Attack, Is.EqualTo(2));
    }

    [Test]
    public void SumPartyAuras_MemberBelowThreshold_NoBonus()
    {
        var members = new List<PartyMember> { Make("sen", 4) }; // below Lv5
        var bonus = CharacterMilestoneLogic.SumPartyAuras(members);
        Assert.That(bonus.Luck, Is.EqualTo(0));
    }

    // ── SumAllMilestoneBonuses ──────────────────────────────────────

    [Test]
    public void SumAllMilestoneBonuses_CombinesIndividualAndAuras()
    {
        var members = new List<PartyMember>
        {
            Make("sen", 10),    // Individual: +3 SPD; Aura: +2 LCK
            Make("lily", 10),   // Aura: +3 RES
        };
        var bonus = CharacterMilestoneLogic.SumAllMilestoneBonuses("sen", 10, members);

        Assert.That(bonus.Speed, Is.EqualTo(3),      "Sen individual +3 SPD");
        Assert.That(bonus.Luck, Is.EqualTo(2),        "Sen aura +2 LCK");
        Assert.That(bonus.Resistance, Is.EqualTo(3),  "Lily aura +3 RES");
    }

    [Test]
    public void SumAllMilestoneBonuses_OwnAuraCountedOnce()
    {
        // Sen at Lv10: individual +3 SPD + aura +2 LCK
        // The aura comes from SumPartyAuras which sees Sen in allMembers
        var members = new List<PartyMember> { Make("sen", 10) };
        var bonus = CharacterMilestoneLogic.SumAllMilestoneBonuses("sen", 10, members);

        Assert.That(bonus.Luck, Is.EqualTo(2), "Own aura counted exactly once");
    }

    // ── HasTag ──────────────────────────────────────────────────────

    [Test]
    public void HasTag_RainGoldBonus_TrueAtLevel15()
    {
        var members = new List<PartyMember> { Make("rain", 15) };
        Assert.That(CharacterMilestoneLogic.HasTag(members, CharacterMilestone.RainGoldBonus), Is.True);
    }

    [Test]
    public void HasTag_RainGoldBonus_FalseAtLevel14()
    {
        var members = new List<PartyMember> { Make("rain", 14) };
        Assert.That(CharacterMilestoneLogic.HasTag(members, CharacterMilestone.RainGoldBonus), Is.False);
    }

    [Test]
    public void HasTag_BhataRepelExtend_TrueAtLevel15()
    {
        var members = new List<PartyMember> { Make("bhata", 15) };
        Assert.That(CharacterMilestoneLogic.HasTag(members, CharacterMilestone.BhataRepelExtend), Is.True);
    }

    [Test]
    public void HasTag_LilyBrewMaster_TrueAtLevel15()
    {
        var members = new List<PartyMember> { Make("lily", 15) };
        Assert.That(CharacterMilestoneLogic.HasTag(members, CharacterMilestone.LilyBrewMaster), Is.True);
    }

    [Test]
    public void HasTag_KrioraShieldWall_TrueAtLevel15()
    {
        var members = new List<PartyMember> { Make("kriora", 15) };
        Assert.That(CharacterMilestoneLogic.HasTag(members, CharacterMilestone.KrioraShieldWall), Is.True);
    }

    [Test]
    public void HasTag_NullMembers_ReturnsFalse()
        => Assert.That(CharacterMilestoneLogic.HasTag(null!, "any"), Is.False);

    [Test]
    public void HasTag_EmptyTag_ReturnsFalse()
    {
        var members = new List<PartyMember> { Make("rain", 20) };
        Assert.That(CharacterMilestoneLogic.HasTag(members, ""), Is.False);
    }
}
