using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Core.Data;

/// <summary>
/// Runtime container for multi-class progression state.
/// Owned by GameManager as an internal domain (like PlayerProgressionData).
/// </summary>
public class MultiClassData
{
    public Dictionary<PlayerClass, ClassProgressionEntry> ClassEntries { get; } = new();

    public PlayerClass ActiveClass { get; private set; } = PlayerClass.Bard;

    /// <summary>
    /// Initialize the starting class from character creation.
    /// </summary>
    public void InitializeStartingClass(PlayerClass cls, ClassProgressionEntry entry)
    {
        ClassEntries[cls] = entry;
        ActiveClass = cls;
    }

    /// <summary>
    /// Snapshot the active class's current stats before switching away.
    /// </summary>
    public void SaveActiveClassState(
        int level, int exp,
        int maxHp, int attack, int defense, int speed,
        int magic, int resistance, int luck, int maxMp)
    {
        if (!ClassEntries.TryGetValue(ActiveClass, out var entry))
            return;

        entry.Level      = level;
        entry.Exp        = exp;
        entry.MaxHp      = maxHp;
        entry.Attack     = attack;
        entry.Defense    = defense;
        entry.Speed      = speed;
        entry.Magic      = magic;
        entry.Resistance = resistance;
        entry.Luck       = luck;
        entry.MaxMp      = maxMp;
    }

    /// <summary>
    /// Switch to a different class. Returns the entry for the target class.
    /// If the class has never been used, <paramref name="defaultEntryFactory"/> creates its initial entry.
    /// </summary>
    public ClassProgressionEntry SwitchTo(
        PlayerClass newClass,
        System.Func<PlayerClass, ClassProgressionEntry>? defaultEntryFactory = null)
    {
        if (!ClassEntries.TryGetValue(newClass, out var entry))
        {
            entry = defaultEntryFactory?.Invoke(newClass)
                ?? new ClassProgressionEntry { Class = newClass };
            ClassEntries[newClass] = entry;
        }

        ActiveClass = newClass;
        return entry;
    }

    /// <summary>
    /// Returns all class levels for cross-class bonus computation.
    /// </summary>
    public Dictionary<PlayerClass, int> GetAllClassLevels()
    {
        return ClassEntries.ToDictionary(kv => kv.Key, kv => kv.Value.Level);
    }

    /// <summary>
    /// Sync the active class's level/exp after level-ups occur.
    /// </summary>
    public void UpdateActiveClassProgression(int level, int exp)
    {
        if (ClassEntries.TryGetValue(ActiveClass, out var entry))
        {
            entry.Level = level;
            entry.Exp = exp;
        }
    }

    public void Reset()
    {
        ClassEntries.Clear();
        ActiveClass = PlayerClass.Bard;
    }

    /// <summary>
    /// Restore from save data.
    /// </summary>
    public void ApplyFromSave(List<ClassProgressionEntry> entries, string activeClassName)
    {
        ClassEntries.Clear();
        foreach (var entry in entries)
            ClassEntries[entry.Class] = entry;

        if (System.Enum.TryParse<PlayerClass>(activeClassName, out var cls))
            ActiveClass = cls;
    }
}
