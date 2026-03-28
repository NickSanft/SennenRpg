using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static quest condition evaluation — no Godot dependency, NUnit-testable.
/// State (flags, kill counts) is injected as parameters so the logic is fully isolated.
/// </summary>
public static class QuestLogic
{
    /// <summary>
    /// Returns true if every condition in the list is currently satisfied.
    /// An empty list is considered fully satisfied (vacuously true).
    /// </summary>
    public static bool AreAllConditionsMet(
        IReadOnlyList<QuestCondition>         conditions,
        IReadOnlyDictionary<string, bool>     flags,
        IReadOnlyDictionary<string, int>      killCounts)
    {
        foreach (var cond in conditions)
        {
            if (!IsSatisfied(cond, flags, killCounts))
                return false;
        }
        return true;
    }

    /// <summary>Evaluates a single condition against the provided game state.</summary>
    public static bool IsSatisfied(
        QuestCondition                        cond,
        IReadOnlyDictionary<string, bool>     flags,
        IReadOnlyDictionary<string, int>      killCounts)
    {
        return cond.Type switch
        {
            QuestConditionType.KillCount =>
                killCounts.TryGetValue(cond.TargetId, out int kills) && kills >= cond.Count,

            QuestConditionType.Flag =>
                flags.TryGetValue(cond.TargetId, out bool flagVal) && flagVal,

            QuestConditionType.TalkTo =>
                flags.TryGetValue($"talked_to_{cond.TargetId}", out bool talked) && talked,

            _ => false,
        };
    }
}
