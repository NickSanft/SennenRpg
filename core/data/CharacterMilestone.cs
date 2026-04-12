namespace SennenRpg.Core.Data;

/// <summary>
/// A per-character milestone bonus earned at a specific level threshold.
/// Individual bonuses buff only the owning character; party-wide (aura) bonuses
/// buff every party member once unlocked, permanently (even from the reserve bench).
/// Stat bonuses reuse <see cref="EquipmentBonuses"/> for pipeline compatibility.
/// Tag-based milestones grant non-combat gameplay effects checked by game systems.
/// </summary>
public readonly record struct CharacterMilestone(
    string MemberId,
    int RequiredLevel,
    string Description,
    bool IsPartyWide,
    EquipmentBonuses StatBonuses = default,
    string? Tag = null)
{
    // ── Tag constants for non-combat effects ─────────────────────────
    /// <summary>Rain Lv15: +25% gold earned from battles.</summary>
    public const string RainGoldBonus = "rain_gold_bonus";
    /// <summary>Bhata Lv15: Repel items last 50% longer.</summary>
    public const string BhataRepelExtend = "bhata_repel_extend";
    /// <summary>Lily Lv15: Cooking Perfect thresholds are relaxed.</summary>
    public const string LilyBrewMaster = "lily_brew_master";
    /// <summary>Kriora Lv15: 15% chance to negate rhythm-phase damage per hit.</summary>
    public const string KrioraShieldWall = "kriora_shield_wall";
}
