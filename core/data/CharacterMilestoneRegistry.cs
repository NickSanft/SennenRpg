namespace SennenRpg.Core.Data;

/// <summary>
/// Static registry of all per-character milestone bonuses. Each character earns
/// four milestones at Lv5, Lv10, Lv15, and Lv20. Lv5 and Lv15 are individual;
/// Lv10 and Lv20 are party-wide auras. Some Lv15 milestones grant non-combat
/// tag effects instead of stat bonuses.
/// </summary>
public static class CharacterMilestoneRegistry
{
    /// <summary>All 20 character milestones (4 per character × 5 characters).</summary>
    public static readonly CharacterMilestone[] All =
    {
        // ── Sen (Bard / multi-class leader) ──────────────────────────
        new("sen",  5,  "+3 SPD (sharpened agility)",
            IsPartyWide: false,
            StatBonuses: new EquipmentBonuses(Speed: 3)),

        new("sen", 10, "+2 LCK aura (leader's fortune)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(Luck: 2)),

        new("sen", 15, "+5 Max HP (battle-hardened resolve)",
            IsPartyWide: false,
            StatBonuses: new EquipmentBonuses(MaxHp: 5)),

        new("sen", 20, "+3 ATK aura (inspiring presence)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(Attack: 3)),

        // ── Lily (Mage / Alchemist specialist) ───────────────────────
        new("lily",  5, "+3 MAG (deepened arcane study)",
            IsPartyWide: false,
            StatBonuses: new EquipmentBonuses(Magic: 3)),

        new("lily", 10, "+3 RES aura (protective ward)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(Resistance: 3)),

        new("lily", 15, "Cooking quality thresholds relaxed",
            IsPartyWide: false,
            Tag: CharacterMilestone.LilyBrewMaster),

        new("lily", 20, "+3 DEF aura (forged-gear wisdom)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(Defense: 3)),

        // ── Rain (Ranger / Rogue specialist) ─────────────────────────
        new("rain",  5, "+3 SPD (ranger swiftness)",
            IsPartyWide: false,
            StatBonuses: new EquipmentBonuses(Speed: 3)),

        new("rain", 10, "+2 SPD aura (scouting advantage)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(Speed: 2)),

        new("rain", 15, "+25% gold from battles",
            IsPartyWide: false,
            Tag: CharacterMilestone.RainGoldBonus),

        new("rain", 20, "+3 LCK aura (fortune favours the bold)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(Luck: 3)),

        // ── Bhata (Ranger specialist) ────────────────────────────────
        new("bhata",  5, "+3 ATK (aggressive combat style)",
            IsPartyWide: false,
            StatBonuses: new EquipmentBonuses(Attack: 3)),

        new("bhata", 10, "+2 ATK aura (rally cry)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(Attack: 2)),

        new("bhata", 15, "Repel items last 50% longer",
            IsPartyWide: false,
            Tag: CharacterMilestone.BhataRepelExtend),

        new("bhata", 20, "+5 Max HP aura (protective presence)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(MaxHp: 5)),

        // ── Kriora (Fighter specialist) ──────────────────────────────
        new("kriora",  5, "+3 DEF (fighter's resilience)",
            IsPartyWide: false,
            StatBonuses: new EquipmentBonuses(Defense: 3)),

        new("kriora", 10, "+2 DEF aura (shield wall)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(Defense: 2)),

        new("kriora", 15, "15% chance to block rhythm damage",
            IsPartyWide: false,
            Tag: CharacterMilestone.KrioraShieldWall),

        new("kriora", 20, "+5 Max HP aura (iron resolve)",
            IsPartyWide: true,
            StatBonuses: new EquipmentBonuses(MaxHp: 5)),
    };
}
