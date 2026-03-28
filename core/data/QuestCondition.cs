namespace SennenRpg.Core.Data;

/// <summary>
/// A single testable condition for quest progress.
/// No Godot dependency — evaluate via <see cref="QuestLogic.IsSatisfied"/>.
/// </summary>
/// <param name="Type">The condition kind.</param>
/// <param name="TargetId">enemyId, flagKey, or npcId depending on Type.</param>
/// <param name="Count">Required count (meaningful for KillCount; defaults to 1).</param>
public sealed record QuestCondition(
    QuestConditionType Type,
    string             TargetId,
    int                Count = 1);
