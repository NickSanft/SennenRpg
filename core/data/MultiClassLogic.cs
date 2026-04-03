using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure static logic for multi-class bonus computation.
/// No Godot dependency — fully NUnit-testable.
/// </summary>
public static class MultiClassLogic
{
    /// <summary>
    /// Returns all cross-class bonuses the player has earned
    /// based on their level in each class.
    /// </summary>
    public static List<CrossClassBonus> GetEarnedBonuses(
        Dictionary<PlayerClass, int> classLevels)
    {
        var earned = new List<CrossClassBonus>();
        foreach (var bonus in CrossClassBonusRegistry.All)
        {
            if (classLevels.TryGetValue(bonus.SourceClass, out int level)
                && level >= bonus.RequiredLevel)
            {
                earned.Add(bonus);
            }
        }
        return earned;
    }

    /// <summary>
    /// Sums all earned cross-class stat bonuses into a single <see cref="EquipmentBonuses"/>.
    /// Feeds directly into the existing effective stats pipeline.
    /// </summary>
    public static EquipmentBonuses SumCrossClassBonuses(
        Dictionary<PlayerClass, int> classLevels)
    {
        var bonusList = GetEarnedBonuses(classLevels)
            .Select(b => b.StatBonuses)
            .ToList();
        return EquipmentLogic.SumBonuses(bonusList);
    }

    /// <summary>
    /// Returns spell paths unlocked across all classes via cross-class bonuses.
    /// </summary>
    public static List<string> GetCrossClassSpells(
        Dictionary<PlayerClass, int> classLevels)
    {
        var spells = new List<string>();
        foreach (var bonus in GetEarnedBonuses(classLevels))
        {
            if (bonus.UnlockedSpellPath != null)
                spells.Add(bonus.UnlockedSpellPath);
        }
        return spells;
    }

    /// <summary>
    /// Creates a <see cref="ClassProgressionEntry"/> snapshot from current player state.
    /// Called before switching away from the active class.
    /// </summary>
    public static ClassProgressionEntry SnapshotToEntry(
        PlayerClass cls, int level, int exp,
        int maxHp, int attack, int defense, int speed,
        int magic, int resistance, int luck, int maxMp)
    {
        return new ClassProgressionEntry
        {
            Class      = cls,
            Level      = level,
            Exp        = exp,
            MaxHp      = maxHp,
            Attack     = attack,
            Defense    = defense,
            Speed      = speed,
            Magic      = magic,
            Resistance = resistance,
            Luck       = luck,
            MaxMp      = maxMp,
        };
    }
}
