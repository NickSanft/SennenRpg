namespace SennenRpg.Core.Data;

/// <summary>
/// One choosable reward offered to the player on quest completion.
/// No Godot dependency.
/// </summary>
/// <param name="Label">Display text shown in the reward-choice screen.</param>
/// <param name="ExpBonus">Additional XP granted on top of the quest base reward.</param>
/// <param name="ItemPath">Optional resource path to an ItemData or EquipmentData asset.</param>
/// <param name="GoldBonus">Optional gold awarded.</param>
public sealed record QuestRewardOption(
    string Label,
    int    ExpBonus  = 0,
    string ItemPath  = "",
    int    GoldBonus = 0);
