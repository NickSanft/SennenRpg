using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Identifies one entry in the multi-actor battle turn order: either a party member
/// (referenced by index into <c>PartyData.Members</c>) or an enemy (referenced by
/// index into the BattleScene's enemies list). Speed and an id-based tiebreak are
/// stored alongside so the queue is fully deterministic for unit testing.
/// </summary>
public readonly record struct TurnQueueEntry(
    bool IsParty,
    int  Index,
    int  Speed);

/// <summary>
/// Pure static logic for the multi-actor battle turn order. Builds a per-round queue
/// of every living actor (party + enemies) sorted by Speed descending, with stable
/// tiebreaks so the order is deterministic. No Godot dependency — fully NUnit-testable.
///
/// To keep this file decoupled from the Godot-flavoured EnemyData / EnemyInstance
/// types, callers pass plain (Speed, IsKO) arrays. BattleScene builds those from its
/// PartyMember and EnemyInstance lists right before calling BuildOrder.
/// </summary>
public static class TurnQueue
{
    /// <summary>
    /// Build the action order for the current round.
    /// Every living actor (HP &gt; 0, IsKO == false) gets one slot.
    /// Sort: Speed descending; ties broken by IsParty (party first) then index ascending,
    /// guaranteeing a stable, repeatable order.
    /// </summary>
    /// <param name="partySpeeds">Per-party-member (Speed, IsKO) — index in this array == PartyMember index.</param>
    /// <param name="enemySpeeds">Per-enemy (Speed, IsKO) — index in this array == enemy instance index.</param>
    public static List<TurnQueueEntry> BuildOrder(
        IList<(int Speed, bool IsKO)> partySpeeds,
        IList<(int Speed, bool IsKO)> enemySpeeds)
    {
        var entries = new List<TurnQueueEntry>();

        if (partySpeeds != null)
        {
            for (int i = 0; i < partySpeeds.Count; i++)
            {
                if (partySpeeds[i].IsKO) continue;
                entries.Add(new TurnQueueEntry(IsParty: true, Index: i, Speed: partySpeeds[i].Speed));
            }
        }

        if (enemySpeeds != null)
        {
            for (int i = 0; i < enemySpeeds.Count; i++)
            {
                if (enemySpeeds[i].IsKO) continue;
                entries.Add(new TurnQueueEntry(IsParty: false, Index: i, Speed: enemySpeeds[i].Speed));
            }
        }

        // Stable sort: Speed desc, then party-before-enemy on a tie, then ascending index.
        entries.Sort((a, b) =>
        {
            int s = b.Speed.CompareTo(a.Speed);
            if (s != 0) return s;
            int p = b.IsParty.CompareTo(a.IsParty); // party first when speeds tie
            if (p != 0) return p;
            return a.Index.CompareTo(b.Index);
        });

        return entries;
    }
}
