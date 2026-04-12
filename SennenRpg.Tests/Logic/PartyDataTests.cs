using NUnit.Framework;
using SennenRpg.Core.Data;

namespace SennenRpg.Tests.Logic;

[TestFixture]
public class PartyDataTests
{
    private PartyData _party = null!;

    [SetUp]
    public void SetUp() => _party = new PartyData();

    private static PartyMember Make(string id, string name = "")
        => new() { MemberId = id, DisplayName = string.IsNullOrEmpty(name) ? id : name };

    [Test]
    public void NewParty_IsEmpty()
    {
        Assert.That(_party.IsEmpty, Is.True);
        Assert.That(_party.Count,   Is.EqualTo(0));
        Assert.That(_party.Leader,  Is.Null);
    }

    [Test]
    public void Add_AppendsAndSetsLeaderToFirst()
    {
        Assert.That(_party.Add(Make("sen", "Sen")), Is.True);
        Assert.That(_party.Count,        Is.EqualTo(1));
        Assert.That(_party.Leader!.MemberId, Is.EqualTo("sen"));
        Assert.That(_party.LeaderIndex,  Is.EqualTo(0));
    }

    [Test]
    public void Add_RejectsDuplicateMemberId()
    {
        _party.Add(Make("sen"));
        Assert.That(_party.Add(Make("sen", "Sen2")), Is.False);
        Assert.That(_party.Count, Is.EqualTo(1));
    }

    [Test]
    public void Add_RespectsMaxMembersCap()
    {
        for (int i = 0; i < PartyData.MaxMembers; i++)
            Assert.That(_party.Add(Make($"m{i}")), Is.True);

        Assert.That(_party.IsFull, Is.True);
        Assert.That(_party.Add(Make("overflow")), Is.False);
        Assert.That(_party.TotalCount, Is.EqualTo(PartyData.MaxMembers));
    }

    [Test]
    public void Add_FifthMember_GoesToReserve()
    {
        for (int i = 0; i < PartyData.MaxActiveMembers; i++)
            _party.Add(Make($"m{i}"));

        Assert.That(_party.ActiveCount, Is.EqualTo(PartyData.MaxActiveMembers));
        Assert.That(_party.IsActiveFull, Is.True);

        _party.Add(Make("reserve1"));
        Assert.That(_party.ActiveCount, Is.EqualTo(PartyData.MaxActiveMembers));
        Assert.That(_party.ReserveMembers.Count, Is.EqualTo(1));
        Assert.That(_party.ReserveMembers[0].MemberId, Is.EqualTo("reserve1"));
        Assert.That(_party.TotalCount, Is.EqualTo(PartyData.MaxActiveMembers + 1));
    }

    [Test]
    public void Remove_RemovesAndAdjustsLeader()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Add(Make("rain"));
        _party.SetLeader("rain");
        Assert.That(_party.LeaderIndex, Is.EqualTo(2));

        // Removing the leader (last member) should clamp the leader index back into range.
        Assert.That(_party.Remove("rain"), Is.True);
        Assert.That(_party.Count,       Is.EqualTo(2));
        Assert.That(_party.LeaderIndex, Is.EqualTo(1));
    }

    [Test]
    public void Remove_NonExistent_ReturnsFalse()
    {
        _party.Add(Make("sen"));
        Assert.That(_party.Remove("ghost"), Is.False);
    }

    [Test]
    public void Remove_FromReserve()
    {
        for (int i = 0; i < PartyData.MaxActiveMembers; i++)
            _party.Add(Make($"m{i}"));
        _party.Add(Make("reserve1"));

        Assert.That(_party.Remove("reserve1"), Is.True);
        Assert.That(_party.ReserveMembers.Count, Is.EqualTo(0));
        Assert.That(_party.TotalCount, Is.EqualTo(PartyData.MaxActiveMembers));
    }

    [Test]
    public void GetById_LooksUpMember()
    {
        _party.Add(Make("sen", "Sen"));
        _party.Add(Make("lily", "Lily"));

        Assert.That(_party.GetById("lily")!.DisplayName, Is.EqualTo("Lily"));
        Assert.That(_party.GetById("ghost"), Is.Null);
        Assert.That(_party.GetById(""),      Is.Null);
        Assert.That(_party.GetById(null!),   Is.Null);
    }

    [Test]
    public void GetById_FindsReserveMember()
    {
        for (int i = 0; i < PartyData.MaxActiveMembers; i++)
            _party.Add(Make($"m{i}"));
        _party.Add(Make("reserve1", "ReserveGuy"));

        Assert.That(_party.GetById("reserve1")!.DisplayName, Is.EqualTo("ReserveGuy"));
    }

    [Test]
    public void Contains_FindsBothActiveAndReserve()
    {
        for (int i = 0; i < PartyData.MaxActiveMembers; i++)
            _party.Add(Make($"m{i}"));
        _party.Add(Make("reserve1"));

        Assert.That(_party.Contains("m0"), Is.True);
        Assert.That(_party.Contains("reserve1"), Is.True);
        Assert.That(_party.Contains("ghost"), Is.False);
    }

    [Test]
    public void SetLeader_ByIdMovesLeaderIndex()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Add(Make("rain"));

        Assert.That(_party.SetLeader("rain"), Is.True);
        Assert.That(_party.LeaderIndex,        Is.EqualTo(2));
        Assert.That(_party.Leader!.MemberId,  Is.EqualTo("rain"));

        Assert.That(_party.SetLeader("ghost"), Is.False);
        Assert.That(_party.LeaderIndex,         Is.EqualTo(2)); // unchanged
    }

    [Test]
    public void SetLeader_RejectsReserveMember()
    {
        for (int i = 0; i < PartyData.MaxActiveMembers; i++)
            _party.Add(Make($"m{i}"));
        _party.Add(Make("reserve1"));

        Assert.That(_party.SetLeader("reserve1"), Is.False);
    }

    [Test]
    public void Swap_ExchangesMembersAndKeepsLeaderHuman()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Add(Make("rain"));
        _party.SetLeader("lily"); // index 1

        _party.Swap(0, 1);

        Assert.That(_party.Members[0].MemberId, Is.EqualTo("lily"));
        Assert.That(_party.Members[1].MemberId, Is.EqualTo("sen"));
        // Leader should still be Lily even though her index moved.
        Assert.That(_party.LeaderIndex,        Is.EqualTo(0));
        Assert.That(_party.Leader!.MemberId,  Is.EqualTo("lily"));
    }

    [Test]
    public void Swap_OutOfRange_NoOp()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Swap(-1, 5);
        _party.Swap(0, 0); // self-swap
        Assert.That(_party.Members[0].MemberId, Is.EqualTo("sen"));
        Assert.That(_party.Members[1].MemberId, Is.EqualTo("lily"));
    }

    // ── Active / Reserve ────────────────────────────────────────────

    [Test]
    public void SwapActiveReserve_SwapsCorrectly()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Add(Make("rain"));
        _party.Add(Make("bhata"));
        _party.Add(Make("reserve1"));

        Assert.That(_party.SwapActiveReserve(1, 0), Is.True); // lily ↔ reserve1

        Assert.That(_party.Members[1].MemberId, Is.EqualTo("reserve1"));
        Assert.That(_party.ReserveMembers[0].MemberId, Is.EqualTo("lily"));
        Assert.That(_party.ActiveCount, Is.EqualTo(4));
    }

    [Test]
    public void SwapActiveReserve_RejectsSen()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Add(Make("rain"));
        _party.Add(Make("bhata"));
        _party.Add(Make("reserve1"));

        Assert.That(_party.SwapActiveReserve(0, 0), Is.False); // sen at index 0
        Assert.That(_party.Members[0].MemberId, Is.EqualTo("sen"));
    }

    [Test]
    public void SwapActiveReserve_InvalidIndices_ReturnsFalse()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));

        Assert.That(_party.SwapActiveReserve(0, 0), Is.False); // no reserve
        Assert.That(_party.SwapActiveReserve(-1, 0), Is.False);
    }

    [Test]
    public void SwapActiveReserve_LeaderSwappedOut_ReassignsToZero()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Add(Make("rain"));
        _party.Add(Make("bhata"));
        _party.Add(Make("reserve1"));
        _party.SetLeader("lily"); // index 1

        _party.SwapActiveReserve(1, 0); // swap lily out

        // Leader should fall back to index 0 (sen)
        Assert.That(_party.LeaderIndex, Is.EqualTo(0));
        Assert.That(_party.Leader!.MemberId, Is.EqualTo("sen"));
    }

    [Test]
    public void MoveToReserve_MovesFromActive()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Add(Make("rain"));

        Assert.That(_party.MoveToReserve("lily"), Is.True);
        Assert.That(_party.ActiveCount, Is.EqualTo(2));
        Assert.That(_party.ReserveMembers.Count, Is.EqualTo(1));
        Assert.That(_party.ReserveMembers[0].MemberId, Is.EqualTo("lily"));
    }

    [Test]
    public void MoveToReserve_RejectsSen()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));

        Assert.That(_party.MoveToReserve("sen"), Is.False);
        Assert.That(_party.ActiveCount, Is.EqualTo(2));
    }

    [Test]
    public void MoveToReserve_RejectsLastActive()
    {
        _party.Add(Make("lily"));

        Assert.That(_party.MoveToReserve("lily"), Is.False);
        Assert.That(_party.ActiveCount, Is.EqualTo(1));
    }

    [Test]
    public void MoveToActive_MovesFromReserve()
    {
        for (int i = 0; i < PartyData.MaxActiveMembers; i++)
            _party.Add(Make($"m{i}"));
        _party.Add(Make("reserve1"));

        // Make room first
        _party.MoveToReserve("m1");
        Assert.That(_party.MoveToActive("reserve1"), Is.True);
        Assert.That(_party.ActiveCount, Is.EqualTo(PartyData.MaxActiveMembers));
        Assert.That(_party.Contains("reserve1"), Is.True);
    }

    [Test]
    public void MoveToActive_RejectsWhenFull()
    {
        for (int i = 0; i < PartyData.MaxActiveMembers; i++)
            _party.Add(Make($"m{i}"));
        _party.Add(Make("reserve1"));

        Assert.That(_party.MoveToActive("reserve1"), Is.False);
    }

    [Test]
    public void AllMembers_ConcatenatesActiveThenReserve()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Add(Make("rain"));
        _party.Add(Make("bhata"));
        _party.Add(Make("reserve1"));

        var all = _party.AllMembers;
        Assert.That(all.Count, Is.EqualTo(5));
        Assert.That(all[0].MemberId, Is.EqualTo("sen"));
        Assert.That(all[3].MemberId, Is.EqualTo("bhata"));
        Assert.That(all[4].MemberId, Is.EqualTo("reserve1"));
    }

    // ── ReplaceAll ──────────────────────────────────────────────────

    [Test]
    public void ReplaceAll_RebuildsList()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));

        var fresh = new[] { Make("rain"), Make("sen"), Make("lily") };
        _party.ReplaceAll(fresh, leaderIndex: 1);

        Assert.That(_party.ActiveCount, Is.EqualTo(3));
        Assert.That(_party.LeaderIndex, Is.EqualTo(1));
        Assert.That(_party.Leader!.MemberId, Is.EqualTo("sen"));
    }

    [Test]
    public void ReplaceAll_DropsDuplicatesAndRespectsCap()
    {
        var members = new System.Collections.Generic.List<PartyMember>();
        for (int i = 0; i < PartyData.MaxMembers + 3; i++)
            members.Add(Make($"m{i}"));
        // duplicate id
        members.Add(Make("m0"));

        _party.ReplaceAll(members);

        Assert.That(_party.TotalCount, Is.EqualTo(PartyData.MaxMembers));
    }

    [Test]
    public void ReplaceAll_WithActiveCount_SplitsCorrectly()
    {
        var members = new[] { Make("sen"), Make("lily"), Make("rain"), Make("bhata"), Make("reserve1") };
        _party.ReplaceAll(members, leaderIndex: 0, activeCount: 4);

        Assert.That(_party.ActiveCount, Is.EqualTo(4));
        Assert.That(_party.ReserveMembers.Count, Is.EqualTo(1));
        Assert.That(_party.ReserveMembers[0].MemberId, Is.EqualTo("reserve1"));
    }

    [Test]
    public void ReplaceAll_LegacyActiveCount_DefaultsToMax4()
    {
        var members = new[] { Make("m0"), Make("m1"), Make("m2"), Make("m3"), Make("m4"), Make("m5") };
        _party.ReplaceAll(members, activeCount: -1); // legacy

        Assert.That(_party.ActiveCount, Is.EqualTo(PartyData.MaxActiveMembers));
        Assert.That(_party.ReserveMembers.Count, Is.EqualTo(2));
    }

    [Test]
    public void Clear_EmptiesParty()
    {
        _party.Add(Make("sen"));
        _party.Add(Make("lily"));
        _party.Clear();

        Assert.That(_party.IsEmpty,    Is.True);
        Assert.That(_party.LeaderIndex, Is.EqualTo(0));
    }
}
