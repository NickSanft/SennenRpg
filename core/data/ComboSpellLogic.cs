using System;
using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure static logic for the Combo Spell (Dual Tech) system.
/// No Godot dependencies — fully NUnit-testable.
/// </summary>
public static class ComboSpellLogic
{
    /// <summary>
    /// Return the two member IDs in alphabetical order.
    /// </summary>
    public static (string A, string B) NormalizePair(string m1, string m2)
    {
        return string.Compare(m1, m2, StringComparison.Ordinal) <= 0
            ? (m1, m2)
            : (m2, m1);
    }

    /// <summary>
    /// Check whether the current turn and the immediately following turn form a valid
    /// combo pair that both members can afford. Returns null when no combo is available.
    /// </summary>
    /// <param name="queue">The turn order for this round.</param>
    /// <param name="currentIndex">Index of the entry whose turn just started.</param>
    /// <param name="getMemberId">Given a TurnQueueEntry.Index for a party member, return its string ID.</param>
    /// <param name="getMemberMp">Given a TurnQueueEntry.Index for a party member, return its current MP.</param>
    public static ComboSpell? FindAvailableCombo(
        IList<TurnQueueEntry> queue,
        int currentIndex,
        Func<int, string> getMemberId,
        Func<int, int> getMemberMp)
    {
        if (queue == null || currentIndex < 0 || currentIndex + 1 >= queue.Count)
            return null;

        var current = queue[currentIndex];
        var next = queue[currentIndex + 1];

        if (!current.IsParty || !next.IsParty)
            return null;

        string idA = getMemberId(current.Index);
        string idB = getMemberId(next.Index);

        var spell = ComboSpellRegistry.Find(idA, idB);
        if (spell == null)
            return null;

        // Determine which MP cost goes to which member.
        var (normA, _) = NormalizePair(idA, idB);
        int mpA, mpB;
        if (idA == normA)
        {
            mpA = getMemberMp(current.Index);
            mpB = getMemberMp(next.Index);
        }
        else
        {
            mpA = getMemberMp(next.Index);
            mpB = getMemberMp(current.Index);
        }

        if (!CanAfford(spell.Value, mpA, mpB))
            return null;

        return spell;
    }

    /// <summary>
    /// Calculate combo spell damage based on the damage type.
    /// </summary>
    public static int ResolveComboSpellDamage(
        ComboSpellType type,
        int attackA, int attackB,
        int magicA, int magicB,
        int targetDef, int targetRes,
        float accuracy = 1.0f)
    {
        double raw = type switch
        {
            ComboSpellType.Physical => (attackA + attackB) * 1.5 * accuracy - targetDef,
            ComboSpellType.Magical  => (magicA + magicB) * 1.5 * accuracy - targetRes,
            ComboSpellType.Hybrid   => (attackA + magicB) * 1.5 * accuracy - (targetDef + targetRes) / 2.0,
            _ => 1
        };

        return Math.Max(1, (int)raw);
    }

    /// <summary>
    /// Check whether both members have enough MP for the combo spell.
    /// mpA corresponds to MemberA, mpB corresponds to MemberB.
    /// </summary>
    public static bool CanAfford(ComboSpell spell, int mpA, int mpB)
    {
        return mpA >= spell.MpCostA && mpB >= spell.MpCostB;
    }
}
