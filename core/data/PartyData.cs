using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Core.Data;

/// <summary>
/// Runtime container for the player's party. Owned by GameManager as an internal domain.
/// Total roster capped at <see cref="MaxMembers"/>. Up to <see cref="MaxActiveMembers"/>
/// can be active (battle, overworld HUD, followers). The rest sit in reserve.
///
/// <para>
/// The party always contains at least one member (Sen, the leader) once a game has been
/// started. The leader must be in the active list.
/// </para>
/// </summary>
public class PartyData
{
    /// <summary>Maximum total roster size.</summary>
    public const int MaxMembers = 6;

    /// <summary>Maximum number of active (battle/overworld) members.</summary>
    public const int MaxActiveMembers = 4;

    private readonly List<PartyMember> _active = new();
    private readonly List<PartyMember> _reserve = new();
    private int _leaderIndex;

    // ── Properties ───────────────────────────────────────────────────

    /// <summary>Active members only (battle, HUD, followers). Most callers want this.</summary>
    public IReadOnlyList<PartyMember> Members => _active;

    /// <summary>All members: active first, then reserve. For menus that show the full roster.</summary>
    public IReadOnlyList<PartyMember> AllMembers
    {
        get
        {
            var all = new List<PartyMember>(_active.Count + _reserve.Count);
            all.AddRange(_active);
            all.AddRange(_reserve);
            return all;
        }
    }

    /// <summary>Reserve members only.</summary>
    public IReadOnlyList<PartyMember> ReserveMembers => _reserve;

    /// <summary>Number of active members.</summary>
    public int Count => _active.Count;

    /// <summary>Alias for <see cref="Count"/>.</summary>
    public int ActiveCount => _active.Count;

    /// <summary>Total members across active + reserve.</summary>
    public int TotalCount => _active.Count + _reserve.Count;

    public bool IsEmpty => _active.Count == 0 && _reserve.Count == 0;

    /// <summary>True when the total roster is at capacity.</summary>
    public bool IsFull => TotalCount >= MaxMembers;

    /// <summary>True when the active list is at capacity.</summary>
    public bool IsActiveFull => _active.Count >= MaxActiveMembers;

    /// <summary>
    /// Index of the current party leader within the active list.
    /// Always within [0, ActiveCount). Returns 0 when empty.
    /// </summary>
    public int LeaderIndex
    {
        get => _active.Count == 0 ? 0 : System.Math.Clamp(_leaderIndex, 0, _active.Count - 1);
        set => _leaderIndex = System.Math.Clamp(value, 0, System.Math.Max(0, _active.Count - 1));
    }

    /// <summary>The current leader, or null when the party is empty.</summary>
    public PartyMember? Leader => _active.Count == 0 ? null : _active[LeaderIndex];

    // ── Mutation ─────────────────────────────────────────────────────

    /// <summary>
    /// Append a member. Goes to active if there's room, otherwise reserve.
    /// Returns false if the total roster is full or a duplicate exists.
    /// </summary>
    public bool Add(PartyMember member)
    {
        if (member == null) return false;
        if (IsFull) return false;
        if (Contains(member.MemberId)) return false;

        if (_active.Count < MaxActiveMembers)
            _active.Add(member);
        else
            _reserve.Add(member);

        return true;
    }

    /// <summary>True when a member with the given id is in either active or reserve.</summary>
    public bool Contains(string memberId)
    {
        if (string.IsNullOrEmpty(memberId)) return false;
        foreach (var m in _active)
            if (m.MemberId == memberId) return true;
        foreach (var m in _reserve)
            if (m.MemberId == memberId) return true;
        return false;
    }

    /// <summary>Remove a member by id from either list. Returns true on success.</summary>
    public bool Remove(string memberId)
    {
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].MemberId == memberId)
            {
                _active.RemoveAt(i);
                if (_leaderIndex >= _active.Count)
                    _leaderIndex = System.Math.Max(0, _active.Count - 1);
                return true;
            }
        }
        for (int i = 0; i < _reserve.Count; i++)
        {
            if (_reserve[i].MemberId == memberId)
            {
                _reserve.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>Look up a member by id in both lists. Returns null when not found.</summary>
    public PartyMember? GetById(string memberId)
    {
        if (string.IsNullOrEmpty(memberId)) return null;
        foreach (var m in _active)
            if (m.MemberId == memberId) return m;
        foreach (var m in _reserve)
            if (m.MemberId == memberId) return m;
        return null;
    }

    /// <summary>Promote a member to leader by id. The member must be in the active list.</summary>
    public bool SetLeader(string memberId)
    {
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].MemberId == memberId)
            {
                _leaderIndex = i;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Swap two members within the active list. Used by the Party Menu reorder action.
    /// Out-of-range indices are no-ops.
    /// </summary>
    public void Swap(int i, int j)
    {
        if (i < 0 || j < 0 || i >= _active.Count || j >= _active.Count || i == j) return;
        (_active[i], _active[j]) = (_active[j], _active[i]);

        // Keep the leader pointing at the same human after the swap.
        if      (_leaderIndex == i) _leaderIndex = j;
        else if (_leaderIndex == j) _leaderIndex = i;
    }

    /// <summary>
    /// Swap one active member with one reserve member.
    /// Returns false on invalid indices.
    /// </summary>
    public bool SwapActiveReserve(int activeIdx, int reserveIdx)
    {
        if (activeIdx < 0 || activeIdx >= _active.Count) return false;
        if (reserveIdx < 0 || reserveIdx >= _reserve.Count) return false;

        // Don't allow swapping Sen to reserve.
        if (_active[activeIdx].MemberId == "sen") return false;

        var temp = _active[activeIdx];
        _active[activeIdx] = _reserve[reserveIdx];
        _reserve[reserveIdx] = temp;

        // If the swapped-out member was the leader, reassign leader to index 0.
        if (_leaderIndex == activeIdx)
            _leaderIndex = 0;

        return true;
    }

    /// <summary>
    /// Move an active member to reserve.
    /// Rejects if only 1 active member remains or if the member is Sen.
    /// </summary>
    public bool MoveToReserve(string memberId)
    {
        if (string.IsNullOrEmpty(memberId)) return false;
        if (memberId == "sen") return false;
        if (_active.Count <= 1) return false;

        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].MemberId == memberId)
            {
                var member = _active[i];
                _active.RemoveAt(i);
                _reserve.Add(member);

                if (_leaderIndex >= _active.Count)
                    _leaderIndex = System.Math.Max(0, _active.Count - 1);

                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Move a reserve member to the active list.
    /// Rejects if the active list is already at <see cref="MaxActiveMembers"/>.
    /// </summary>
    public bool MoveToActive(string memberId)
    {
        if (string.IsNullOrEmpty(memberId)) return false;
        if (_active.Count >= MaxActiveMembers) return false;

        for (int i = 0; i < _reserve.Count; i++)
        {
            if (_reserve[i].MemberId == memberId)
            {
                var member = _reserve[i];
                _reserve.RemoveAt(i);
                _active.Add(member);
                return true;
            }
        }
        return false;
    }

    /// <summary>Clear all members from both lists.</summary>
    public void Clear()
    {
        _active.Clear();
        _reserve.Clear();
        _leaderIndex = 0;
    }

    /// <summary>
    /// Replace all members. The first <paramref name="activeCount"/> go to active,
    /// the rest go to reserve. Resets <see cref="LeaderIndex"/>.
    /// </summary>
    public void ReplaceAll(IEnumerable<PartyMember> members, int leaderIndex = 0, int activeCount = -1)
    {
        _active.Clear();
        _reserve.Clear();

        var list = new List<PartyMember>();
        var seen = new HashSet<string>();
        foreach (var m in members)
        {
            if (m == null) continue;
            if (list.Count >= MaxMembers) break;
            if (seen.Contains(m.MemberId)) continue;
            seen.Add(m.MemberId);
            list.Add(m);
        }

        // If activeCount is -1 (legacy/default), all go active (clamped to MaxActiveMembers).
        int effectiveActive = activeCount < 0
            ? System.Math.Min(list.Count, MaxActiveMembers)
            : System.Math.Clamp(activeCount, 0, System.Math.Min(list.Count, MaxActiveMembers));

        for (int i = 0; i < list.Count; i++)
        {
            if (i < effectiveActive)
                _active.Add(list[i]);
            else
                _reserve.Add(list[i]);
        }

        _leaderIndex = System.Math.Clamp(leaderIndex, 0, System.Math.Max(0, _active.Count - 1));
    }
}
