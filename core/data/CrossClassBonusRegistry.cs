namespace SennenRpg.Core.Data;

/// <summary>
/// Static registry of all cross-class bonuses. These apply to ALL classes
/// once the player reaches the required level in the source class.
/// Placeholder values — tune later.
/// </summary>
public static class CrossClassBonusRegistry
{
    public static readonly CrossClassBonus[] All =
    [
        // Fighter bonuses
        new(PlayerClass.Fighter, 5, "Fighter Lv5: +5 ATK",
            StatBonuses: new EquipmentBonuses(Attack: 5)),
        new(PlayerClass.Fighter, 10, "Fighter Lv10: +5 DEF",
            StatBonuses: new EquipmentBonuses(Defense: 5)),

        // Bard bonuses
        new(PlayerClass.Bard, 5, "Bard Lv5: Shadow Bolt",
            UnlockedSpellPath: "res://resources/spells/shadow_bolt.tres"),
        new(PlayerClass.Bard, 10, "Bard Lv10: +3 SPD",
            StatBonuses: new EquipmentBonuses(Speed: 3)),

        // Ranger bonuses
        new(PlayerClass.Ranger, 5, "Ranger Lv5: Forager's Eye (+1 forage item, longer rhythm prompt)",
            Tag: CrossClassBonus.ForagersEye),
        new(PlayerClass.Ranger, 10, "Ranger Lv10: +5 LCK",
            StatBonuses: new EquipmentBonuses(Luck: 5)),

        // Mage bonuses
        new(PlayerClass.Mage, 5, "Mage Lv5: +5 MAG",
            StatBonuses: new EquipmentBonuses(Magic: 5)),
        new(PlayerClass.Mage, 10, "Mage Lv10: +5 RES",
            StatBonuses: new EquipmentBonuses(Resistance: 5)),

        // Rogue bonuses
        new(PlayerClass.Rogue, 5, "Rogue Lv5: Lucky Forager (+1 forage roll quality)",
            Tag: CrossClassBonus.LuckyForager),
        new(PlayerClass.Rogue, 10, "Rogue Lv10: Backstab",
            UnlockedSpellPath: "res://resources/spells/backstab.tres"),

        // Alchemist bonuses
        new(PlayerClass.Alchemist, 5, "Alchemist Lv5: Wealth Aura (+20% gold drops)",
            Tag: CrossClassBonus.WealthAura),
        new(PlayerClass.Alchemist, 10, "Alchemist Lv10: Master Brewer (cooking Perfect window widens)",
            Tag: CrossClassBonus.MasterBrewer),
    ];
}
