using Godot;
using System;
using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Resource asset that fully describes a quest: its conditions and reward choices.
/// Serialised as a .tres file; conditions and rewards use parallel string/int arrays
/// because Godot 4's C# serialiser cannot export generic record types directly.
/// </summary>
[GlobalClass]
public partial class QuestData : Resource
{
    [Export] public string QuestId      { get; set; } = "";
    [Export] public string Title        { get; set; } = "";
    [Export] public string Description  { get; set; } = "";
    [Export] public int    BaseExpReward { get; set; } = 100;

    // ── Conditions (parallel arrays) ──────────────────────────────────
    [Export] public string[] ConditionTypes     { get; set; } = Array.Empty<string>();
    [Export] public string[] ConditionTargetIds { get; set; } = Array.Empty<string>();
    [Export] public int[]    ConditionCounts    { get; set; } = Array.Empty<int>();

    // ── Reward options (parallel arrays) ─────────────────────────────
    [Export] public string[] RewardLabels     { get; set; } = Array.Empty<string>();
    [Export] public int[]    RewardExpBonuses  { get; set; } = Array.Empty<int>();
    [Export] public string[] RewardItemPaths   { get; set; } = Array.Empty<string>();
    [Export] public int[]    RewardGoldBonuses { get; set; } = Array.Empty<int>();

    // ── Runtime deserialisers ─────────────────────────────────────────

    /// <summary>Returns typed quest conditions built from the exported parallel arrays.</summary>
    public IReadOnlyList<QuestCondition> GetConditions()
    {
        var list = new List<QuestCondition>();
        for (int i = 0; i < ConditionTypes.Length; i++)
        {
            if (!Enum.TryParse<QuestConditionType>(ConditionTypes[i], out var type)) continue;
            string targetId = i < ConditionTargetIds.Length ? ConditionTargetIds[i] : "";
            int    count    = i < ConditionCounts.Length    ? ConditionCounts[i]    : 1;
            list.Add(new QuestCondition(type, targetId, count));
        }
        return list;
    }

    /// <summary>Returns typed reward options built from the exported parallel arrays.</summary>
    public IReadOnlyList<QuestRewardOption> GetRewards()
    {
        var list = new List<QuestRewardOption>();
        for (int i = 0; i < RewardLabels.Length; i++)
        {
            list.Add(new QuestRewardOption(
                Label:     RewardLabels[i],
                ExpBonus:  i < RewardExpBonuses.Length  ? RewardExpBonuses[i]  : 0,
                ItemPath:  i < RewardItemPaths.Length   ? RewardItemPaths[i]   : "",
                GoldBonus: i < RewardGoldBonuses.Length ? RewardGoldBonuses[i] : 0));
        }
        return list;
    }
}
