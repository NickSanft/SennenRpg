using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure static helpers for the party system. Used by GameManager and the (Phase 7)
/// multi-actor BattleScene to distribute XP, roll level-ups, and answer party-wide
/// queries. No Godot dependency — fully NUnit-testable.
/// </summary>
public static class PartyMemberLogic
{
    /// <summary>
    /// Distributes a flat XP pool across the supplied members. Per the design plan,
    /// every active member receives an equal share — including KO'd members, who
    /// still get XP for surviving to the end of a fight (Q4 in the plan).
    /// Remainder XP from integer division is awarded to the first members in the list
    /// so that the total handed out always equals <paramref name="totalXp"/>.
    /// </summary>
    public static void DistributeXp(IList<PartyMember> activeMembers, int totalXp)
    {
        if (activeMembers == null || activeMembers.Count == 0 || totalXp <= 0) return;

        int share     = totalXp / activeMembers.Count;
        int remainder = totalXp - share * activeMembers.Count;

        for (int i = 0; i < activeMembers.Count; i++)
        {
            int gain = share + (i < remainder ? 1 : 0);
            if (gain > 0) activeMembers[i].Exp += gain;
        }
    }

    /// <summary>
    /// Returns the count of party members that are still standing (CurrentHp &gt; 0).
    /// </summary>
    public static int LivingCount(IList<PartyMember> members)
    {
        if (members == null) return 0;
        int count = 0;
        foreach (var m in members)
            if (m != null && !m.IsKO) count++;
        return count;
    }

    /// <summary>True when every member is KO'd — the trigger condition for game over.</summary>
    public static bool AllKO(IList<PartyMember> members)
        => members != null && members.Count > 0 && LivingCount(members) == 0;

    /// <summary>
    /// Restores a member to full HP and MP. Used by post-battle/full-rest flows
    /// (e.g. Mapp Tavern's "Rest" option, future revive items).
    /// </summary>
    public static void FullHeal(PartyMember member)
    {
        if (member == null) return;
        member.CurrentHp = member.MaxHp;
        member.CurrentMp = member.MaxMp;
    }
}
