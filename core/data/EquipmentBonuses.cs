namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-C# value type (no Godot dependency) used by EquipmentLogic.
/// EquipmentData.Bonuses converts to this for testable calculations.
/// </summary>
public readonly record struct EquipmentBonuses(
    int MaxHp      = 0,
    int Attack     = 0,
    int Defense    = 0,
    int Magic      = 0,
    int Resistance = 0,
    int Speed      = 0,
    int Luck       = 0);
