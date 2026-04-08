using NUnit.Framework;
using SennenRpg.Core.Data;
using System.Collections.Generic;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class PartyMemberLogicTests
{
    private static PartyMember Member(string id, int hp = 30, int maxHp = 30)
        => new()
        {
            MemberId  = id,
            MaxHp     = maxHp,
            CurrentHp = hp,
            MaxMp     = 10,
            CurrentMp = 5,
        };

    // ── DistributeXp ──────────────────────────────────────────────────

    [Test]
    public void DistributeXp_SplitsEvenlyAcrossMembers()
    {
        var party = new List<PartyMember> { Member("sen"), Member("lily"), Member("rain") };
        PartyMemberLogic.DistributeXp(party, totalXp: 30);
        Assert.That(party[0].Exp, Is.EqualTo(10));
        Assert.That(party[1].Exp, Is.EqualTo(10));
        Assert.That(party[2].Exp, Is.EqualTo(10));
    }

    [Test]
    public void DistributeXp_RemainderGoesToFirstMembers()
    {
        var party = new List<PartyMember> { Member("sen"), Member("lily"), Member("rain") };
        PartyMemberLogic.DistributeXp(party, totalXp: 32);
        // 32 / 3 = 10 remainder 2 → first two members get +1.
        Assert.That(party[0].Exp, Is.EqualTo(11));
        Assert.That(party[1].Exp, Is.EqualTo(11));
        Assert.That(party[2].Exp, Is.EqualTo(10));
        Assert.That(party[0].Exp + party[1].Exp + party[2].Exp, Is.EqualTo(32));
    }

    [Test]
    public void DistributeXp_KOdMembersStillReceiveXp()
    {
        var sen  = Member("sen");
        var lily = Member("lily", hp: 0); // KO'd
        var rain = Member("rain");
        var party = new List<PartyMember> { sen, lily, rain };

        PartyMemberLogic.DistributeXp(party, totalXp: 30);

        // Per Q4 in the plan: KO'd members still receive XP.
        Assert.That(sen.Exp,  Is.EqualTo(10));
        Assert.That(lily.Exp, Is.EqualTo(10));
        Assert.That(rain.Exp, Is.EqualTo(10));
    }

    [Test]
    public void DistributeXp_HandlesEmptyOrZero()
    {
        Assert.DoesNotThrow(() => PartyMemberLogic.DistributeXp(new List<PartyMember>(), 100));
        Assert.DoesNotThrow(() => PartyMemberLogic.DistributeXp(new List<PartyMember> { Member("sen") }, 0));
        Assert.DoesNotThrow(() => PartyMemberLogic.DistributeXp(null!, 100));
    }

    // ── LivingCount / AllKO ───────────────────────────────────────────

    [Test]
    public void LivingCount_CountsNonKOdMembers()
    {
        var party = new List<PartyMember>
        {
            Member("sen"),
            Member("lily", hp: 0),
            Member("rain"),
        };
        Assert.That(PartyMemberLogic.LivingCount(party), Is.EqualTo(2));
    }

    [Test]
    public void AllKO_TrueOnlyWhenEveryMemberDown()
    {
        var party = new List<PartyMember>
        {
            Member("sen", hp: 0),
            Member("lily", hp: 0),
        };
        Assert.That(PartyMemberLogic.AllKO(party), Is.True);

        party[0].CurrentHp = 1;
        Assert.That(PartyMemberLogic.AllKO(party), Is.False);
    }

    [Test]
    public void AllKO_EmptyPartyIsFalse()
    {
        Assert.That(PartyMemberLogic.AllKO(new List<PartyMember>()), Is.False);
    }

    // ── FullHeal ──────────────────────────────────────────────────────

    [Test]
    public void FullHeal_RestoresHpAndMpToMax()
    {
        var sen = Member("sen", hp: 5, maxHp: 30);
        sen.CurrentMp = 0;
        sen.MaxMp = 12;

        PartyMemberLogic.FullHeal(sen);

        Assert.That(sen.CurrentHp, Is.EqualTo(30));
        Assert.That(sen.CurrentMp, Is.EqualTo(12));
    }

    [Test]
    public void FullHeal_NullSafe()
    {
        Assert.DoesNotThrow(() => PartyMemberLogic.FullHeal(null!));
    }
}
