using System;
using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static mad-lib equipment generator for Lily's forge in Mellyr Outpost.
/// All methods are side-effect-free and safe to call in NUnit tests.
/// </summary>
public static class LilyForgeLogic
{
    private static readonly string[] Prefixes =
    {
        "Ancient", "Gleaming", "Rusted", "Cursed", "Enchanted",
        "Tattered", "Forbidden", "Radiant", "Worm-Eaten", "Peculiar"
    };

    private static readonly string[] Materials =
    {
        "Iron", "Silver", "Bone", "Crystal", "Shadow",
        "Golden", "Wooden", "Obsidian", "Silk", "Copper"
    };

    private static readonly Dictionary<EquipmentSlot, string[]> SlotNames = new()
    {
        { EquipmentSlot.Weapon,    new[] { "Sword", "Dagger", "Staff", "Axe", "Wand"    } },
        { EquipmentSlot.Shield,    new[] { "Shield", "Buckler", "Aegis"                  } },
        { EquipmentSlot.Head,      new[] { "Helmet", "Crown", "Hood", "Circlet"          } },
        { EquipmentSlot.Body,      new[] { "Armour", "Robe", "Vest", "Coat"              } },
        { EquipmentSlot.Legs,      new[] { "Greaves", "Trousers", "Skirt", "Leggings"   } },
        { EquipmentSlot.Gloves,    new[] { "Gauntlets", "Gloves", "Wraps"               } },
        { EquipmentSlot.Boots,     new[] { "Boots", "Sandals", "Slippers"               } },
        { EquipmentSlot.Accessory, new[] { "Ring", "Amulet", "Charm", "Brooch"          } },
    };

    // ── Recipe generation ─────────────────────────────────────────────────────

    /// <summary>
    /// Generates a compact recipe string: "seed|slot_int|playerLevel".
    /// Player level is baked in so item power is fixed at generation time.
    /// </summary>
    public static string GenerateRecipe(int playerLevel, Func<int>? seedSource = null)
    {
        int seed = seedSource != null ? seedSource() : Environment.TickCount;
        int slot = new Random(seed).Next(0, 8);
        return $"{seed}|{slot}|{playerLevel}";
    }

    /// <summary>
    /// Resolves a recipe string into a <see cref="DynamicEquipmentSave"/>.
    /// Same seed + playerLevel always produces the same item.
    /// </summary>
    public static DynamicEquipmentSave Resolve(string recipe)
    {
        var parts   = recipe.Split('|');
        int seed    = int.Parse(parts[0]);
        var slot    = (EquipmentSlot)int.Parse(parts[1]);
        int level   = parts.Length > 2 ? int.Parse(parts[2]) : 1;
        var rng     = new Random(seed);

        string prefix   = Pick(rng, Prefixes);
        string material = Pick(rng, Materials);
        string typeName = Pick(rng, SlotNames[slot]);

        int budget = rng.Next(5, 16) + (int)(level * 1.5f);

        int atk = 0, def = 0, mag = 0, hp = 0, spd = 0, lck = 0;
        AllocateBonuses(rng, slot, budget, ref atk, ref def, ref mag, ref hp, ref spd, ref lck);

        return new DynamicEquipmentSave
        {
            Id           = $"lily_{seed}",
            DisplayName  = $"{prefix} {material} {typeName}",
            Description  = $"Forged by Lily. \"{prefix}\" is a strong word, but here we are.",
            Slot         = slot,
            BonusAttack  = atk,
            BonusDefense = def,
            BonusMagic   = mag,
            BonusMaxHp   = hp,
            BonusSpeed   = spd,
            BonusLuck    = lck,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static T Pick<T>(Random rng, T[] arr) => arr[rng.Next(arr.Length)];

    private static void AllocateBonuses(
        Random rng, EquipmentSlot slot, int budget,
        ref int atk, ref int def, ref int mag,
        ref int hp, ref int spd, ref int lck)
    {
        while (budget > 0)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    if (rng.Next(2) == 0) atk++; else mag++;
                    break;
                case EquipmentSlot.Accessory:
                    int r = rng.Next(3);
                    if (r == 0) spd++; else if (r == 1) lck++; else hp++;
                    break;
                default:
                    if (rng.Next(2) == 0) def++; else hp++;
                    break;
            }
            budget--;
        }
    }
}
