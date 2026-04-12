using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure computation engine for per-character milestones. All methods are static
/// and side-effect-free — fully NUnit-testable with no Godot runtime dependency.
/// Mirrors the <see cref="MultiClassLogic"/> pattern for cross-class bonuses.
/// </summary>
public static class CharacterMilestoneLogic
{
    /// <summary>
    /// Returns all milestones earned by a specific character at their current level.
    /// </summary>
    public static List<CharacterMilestone> GetEarnedMilestones(string memberId, int level)
    {
        var result = new List<CharacterMilestone>();
        if (string.IsNullOrEmpty(memberId)) return result;

        foreach (var m in CharacterMilestoneRegistry.All)
        {
            if (m.MemberId == memberId && level >= m.RequiredLevel)
                result.Add(m);
        }
        return result;
    }

    /// <summary>
    /// Returns milestones unlocked at exactly the given level for a member.
    /// Used by LevelUpScreen to flash newly earned milestones.
    /// </summary>
    public static List<CharacterMilestone> GetMilestonesAtLevel(string memberId, int level)
    {
        var result = new List<CharacterMilestone>();
        if (string.IsNullOrEmpty(memberId)) return result;

        foreach (var m in CharacterMilestoneRegistry.All)
        {
            if (m.MemberId == memberId && m.RequiredLevel == level)
                result.Add(m);
        }
        return result;
    }

    /// <summary>
    /// Sums the individual (non-aura) milestone stat bonuses for a specific character.
    /// Excludes party-wide auras — those are handled by <see cref="SumPartyAuras"/>.
    /// </summary>
    public static EquipmentBonuses SumIndividualMilestones(string memberId, int level)
    {
        int hp = 0, atk = 0, def = 0, mag = 0, res = 0, spd = 0, lck = 0;

        foreach (var m in CharacterMilestoneRegistry.All)
        {
            if (m.MemberId != memberId || m.IsPartyWide || level < m.RequiredLevel) continue;
            hp  += m.StatBonuses.MaxHp;
            atk += m.StatBonuses.Attack;
            def += m.StatBonuses.Defense;
            mag += m.StatBonuses.Magic;
            res += m.StatBonuses.Resistance;
            spd += m.StatBonuses.Speed;
            lck += m.StatBonuses.Luck;
        }

        return new EquipmentBonuses(hp, atk, def, mag, res, spd, lck);
    }

    /// <summary>
    /// Sums all party-wide aura milestone stat bonuses from all members.
    /// Includes both active and reserve members — auras persist once earned.
    /// </summary>
    public static EquipmentBonuses SumPartyAuras(IReadOnlyList<PartyMember> allMembers)
    {
        if (allMembers == null) return default;

        int hp = 0, atk = 0, def = 0, mag = 0, res = 0, spd = 0, lck = 0;

        foreach (var member in allMembers)
        {
            if (member == null) continue;

            foreach (var m in CharacterMilestoneRegistry.All)
            {
                if (m.MemberId != member.MemberId || !m.IsPartyWide) continue;

                int level = member.MemberId == "sen"
                    ? member.Level // Sen's PartyMember level is synced from GameManager
                    : member.Level;

                if (level < m.RequiredLevel) continue;

                hp  += m.StatBonuses.MaxHp;
                atk += m.StatBonuses.Attack;
                def += m.StatBonuses.Defense;
                mag += m.StatBonuses.Magic;
                res += m.StatBonuses.Resistance;
                spd += m.StatBonuses.Speed;
                lck += m.StatBonuses.Luck;
            }
        }

        return new EquipmentBonuses(hp, atk, def, mag, res, spd, lck);
    }

    /// <summary>
    /// Total milestone bonuses for a specific character: their individual bonuses
    /// plus all party-wide auras from all members (including themselves).
    /// </summary>
    public static EquipmentBonuses SumAllMilestoneBonuses(
        string memberId, int memberLevel,
        IReadOnlyList<PartyMember> allMembers)
    {
        var individual = SumIndividualMilestones(memberId, memberLevel);
        var auras      = SumPartyAuras(allMembers);

        return new EquipmentBonuses(
            MaxHp:      individual.MaxHp      + auras.MaxHp,
            Attack:     individual.Attack     + auras.Attack,
            Defense:    individual.Defense    + auras.Defense,
            Magic:      individual.Magic      + auras.Magic,
            Resistance: individual.Resistance + auras.Resistance,
            Speed:      individual.Speed      + auras.Speed,
            Luck:       individual.Luck       + auras.Luck);
    }

    /// <summary>
    /// Returns true if any member in the party has earned a milestone with the given tag.
    /// Checks all members (active + reserve) since tag effects persist once earned.
    /// </summary>
    public static bool HasTag(IReadOnlyList<PartyMember> allMembers, string tag)
    {
        if (allMembers == null || string.IsNullOrEmpty(tag)) return false;

        foreach (var member in allMembers)
        {
            if (member == null) continue;
            foreach (var m in CharacterMilestoneRegistry.All)
            {
                if (m.Tag == tag && m.MemberId == member.MemberId && member.Level >= m.RequiredLevel)
                    return true;
            }
        }
        return false;
    }
}
