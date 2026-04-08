using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Runtime container for the player's party. Owned by GameManager as an internal domain.
/// Capped at <see cref="MaxMembers"/> active members; everyone in the list is battle-active
/// in the v1 design (no separate reserve list).
///
/// <para>
/// The party always contains at least one member (Sen, the leader) once a game has been
/// started. Member at index 0 is the *leader* by default; <see cref="LeaderIndex"/> tracks
/// the current leader for parties that have re-ordered themselves.
/// </para>
/// </summary>
public class PartyData
{
    /// <summary>Maximum number of party members. Confirmed in the design plan as 6.</summary>
    public const int MaxMembers = 6;

    private readonly List<PartyMember> _members = new();
    private int _leaderIndex;

    public IReadOnlyList<PartyMember> Members => _members;
    public int Count => _members.Count;
    public bool IsEmpty => _members.Count == 0;
    public bool IsFull  => _members.Count >= MaxMembers;

    /// <summary>
    /// Index of the current party leader. Always within [0, Count).
    /// Returns 0 when the party is empty (callers must guard with <see cref="IsEmpty"/>).
    /// </summary>
    public int LeaderIndex
    {
        get => _members.Count == 0 ? 0 : System.Math.Clamp(_leaderIndex, 0, _members.Count - 1);
        set => _leaderIndex = System.Math.Clamp(value, 0, System.Math.Max(0, _members.Count - 1));
    }

    /// <summary>The current leader, or null when the party is empty.</summary>
    public PartyMember? Leader => _members.Count == 0 ? null : _members[LeaderIndex];

    // ── Mutation ──────────────────────────────────────────────────────

    /// <summary>
    /// Append a member. Returns false if the party is already full or a member with the
    /// same <see cref="PartyMember.MemberId"/> already exists.
    /// </summary>
    public bool Add(PartyMember member)
    {
        if (member == null) return false;
        if (IsFull) return false;
        if (Contains(member.MemberId)) return false;
        _members.Add(member);
        return true;
    }

    /// <summary>True when a member with the given id is already in the party.</summary>
    public bool Contains(string memberId)
    {
        if (string.IsNullOrEmpty(memberId)) return false;
        foreach (var m in _members)
            if (m.MemberId == memberId) return true;
        return false;
    }

    /// <summary>Remove a member by id. Returns true on success.</summary>
    public bool Remove(string memberId)
    {
        for (int i = 0; i < _members.Count; i++)
        {
            if (_members[i].MemberId == memberId)
            {
                _members.RemoveAt(i);
                if (_leaderIndex >= _members.Count) _leaderIndex = System.Math.Max(0, _members.Count - 1);
                return true;
            }
        }
        return false;
    }

    /// <summary>Look up a member by id. Returns null when not found.</summary>
    public PartyMember? GetById(string memberId)
    {
        if (string.IsNullOrEmpty(memberId)) return null;
        foreach (var m in _members)
            if (m.MemberId == memberId) return m;
        return null;
    }

    /// <summary>Promote a member to leader by id. Returns true on success.</summary>
    public bool SetLeader(string memberId)
    {
        for (int i = 0; i < _members.Count; i++)
        {
            if (_members[i].MemberId == memberId)
            {
                _leaderIndex = i;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Swap two members in the list. Used by the Party Menu reorder action.
    /// Out-of-range indices are no-ops.
    /// </summary>
    public void Swap(int i, int j)
    {
        if (i < 0 || j < 0 || i >= _members.Count || j >= _members.Count || i == j) return;
        (_members[i], _members[j]) = (_members[j], _members[i]);

        // Keep the leader pointing at the same human after the swap.
        if      (_leaderIndex == i) _leaderIndex = j;
        else if (_leaderIndex == j) _leaderIndex = i;
    }

    /// <summary>Clear all members. Used by ResetForNewGame and load-game flows.</summary>
    public void Clear()
    {
        _members.Clear();
        _leaderIndex = 0;
    }

    /// <summary>Replace all members with the supplied list. Resets <see cref="LeaderIndex"/>.</summary>
    public void ReplaceAll(IEnumerable<PartyMember> members, int leaderIndex = 0)
    {
        _members.Clear();
        foreach (var m in members)
        {
            if (_members.Count >= MaxMembers) break;
            if (m == null || Contains(m.MemberId)) continue;
            _members.Add(m);
        }
        _leaderIndex = System.Math.Clamp(leaderIndex, 0, System.Math.Max(0, _members.Count - 1));
    }
}
