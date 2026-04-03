namespace SennenRpg.Core.Data;

/// <summary>
/// Defines a single cross-class passive bonus earned at a specific class level.
/// Stat bonuses reuse <see cref="EquipmentBonuses"/> so they slot into the existing
/// effective stats pipeline alongside equipment.
/// </summary>
public readonly record struct CrossClassBonus(
    PlayerClass SourceClass,
    int RequiredLevel,
    string Description,
    EquipmentBonuses StatBonuses = default,
    string? UnlockedSpellPath = null);
