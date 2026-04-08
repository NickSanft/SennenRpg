using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Snapshot of a single party member's effective combat stats — base values
/// plus the sum of equipment / cross-class bonuses applied to them.
/// Plain ints (no Godot dependency) for unit testing and clean save/load.
/// </summary>
public readonly record struct PartyMemberEffectiveStats(
    int MaxHp,
    int CurrentHp,
    int MaxMp,
    int CurrentMp,
    int Attack,
    int Defense,
    int Speed,
    int Magic,
    int Resistance,
    int Luck);

/// <summary>
/// Pure static helpers for party-member stat math. Used by the (Phase 6) party-aware
/// Stats and Equipment menus so each member's effective stats can be displayed
/// without going through the GameManager facade (which is hard-wired to Sen).
/// </summary>
public static class PartyMemberStatsLogic
{
    /// <summary>
    /// Compute effective stats for a member by adding equipment / cross-class bonuses
    /// to their base stats. Pass <see cref="EquipmentBonuses"/> entries pre-summed,
    /// or use <see cref="EquipmentLogic.SumBonuses"/> on the member's equipped items.
    /// </summary>
    public static PartyMemberEffectiveStats ComputeEffective(PartyMember member, EquipmentBonuses bonuses)
    {
        if (member == null)
            return default;

        return new PartyMemberEffectiveStats(
            MaxHp:      member.MaxHp      + bonuses.MaxHp,
            CurrentHp:  member.CurrentHp,
            MaxMp:      member.MaxMp,
            CurrentMp:  member.CurrentMp,
            Attack:     member.Attack     + bonuses.Attack,
            Defense:    member.Defense    + bonuses.Defense,
            Speed:      member.Speed      + bonuses.Speed,
            Magic:      member.Magic      + bonuses.Magic,
            Resistance: member.Resistance + bonuses.Resistance,
            Luck:       member.Luck       + bonuses.Luck);
    }

    /// <summary>
    /// Convenience overload that takes a list of pre-converted equipment bonuses,
    /// sums them via <see cref="EquipmentLogic.SumBonuses"/>, and returns the resulting
    /// effective stat snapshot. Used by the menus when iterating per-member equipped items.
    /// </summary>
    public static PartyMemberEffectiveStats ComputeEffective(PartyMember member, IEnumerable<EquipmentBonuses> equippedBonuses)
    {
        return ComputeEffective(member, EquipmentLogic.SumBonuses(equippedBonuses));
    }
}
