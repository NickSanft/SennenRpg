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
    string? UnlockedSpellPath = null,
    /// <summary>
    /// Optional gameplay tag for system-specific lookups (e.g. <see cref="ForagersEye"/>).
    /// Use <see cref="MultiClassLogic.HasTag"/> to query.
    /// </summary>
    string? Tag = null)
{
    /// <summary>Tag for the Ranger Lv5 Forager's Eye bonus — extends the foraging minigame.</summary>
    public const string ForagersEye = "forager_eye";
}
