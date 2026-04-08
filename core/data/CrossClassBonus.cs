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

    /// <summary>Tag for the Rogue Lv5 Lucky Forager bonus — biases foraging rolls toward rarer items.</summary>
    public const string LuckyForager = "lucky_forager";

    /// <summary>Tag for the Alchemist Lv5 Wealth Aura bonus — multiplies gold drops.</summary>
    public const string WealthAura = "wealth_aura";

    /// <summary>Tag for the Alchemist Lv10 Master Brewer bonus — widens the cooking-minigame Perfect window.</summary>
    public const string MasterBrewer = "master_brewer";
}
