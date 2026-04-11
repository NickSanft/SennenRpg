using System;
using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure-static crystal weapon generator for Kriora's passive ability in Mellyr Outpost.
/// Produces Magic-biased Weapons and Accessories with crystal-themed names.
/// All methods are side-effect-free and safe to call in NUnit tests.
/// </summary>
public static class KrioraForgeLogic
{
    private static readonly string[] Prefixes =
    {
        "Prismatic", "Shardglass", "Resonant", "Frozen-Light", "Veilstone",
        "Arcane", "Glinting", "Fractured", "Starlit", "Ethereal"
    };

    private static readonly string[] Materials =
    {
        "Crystal", "Amethyst", "Quartz", "Sapphire", "Diamond",
        "Opal", "Moonstone", "Obsidian", "Topaz", "Ruby"
    };

    private static readonly Dictionary<EquipmentSlot, string[]> SlotNames = new()
    {
        { EquipmentSlot.Weapon,    new[] { "Staff", "Wand", "Orb", "Scepter", "Focus" } },
        { EquipmentSlot.Accessory, new[] { "Crystal", "Pendant", "Ring", "Prism", "Shard" } },
    };

    /// <summary>The two equipment slots Kriora's forge can produce.</summary>
    private static readonly EquipmentSlot[] AllowedSlots =
    {
        EquipmentSlot.Weapon, EquipmentSlot.Accessory
    };

    // ── Recipe generation ─────────────────────────────────────────────────────

    /// <summary>
    /// Generates a compact recipe string: "seed|slot_int|playerLevel".
    /// Player level is baked in so item power is fixed at generation time.
    /// </summary>
    public static string GenerateRecipe(int playerLevel, Func<int>? seedSource = null)
    {
        int seed = seedSource != null ? seedSource() : Environment.TickCount;
        var rng  = new Random(seed);
        var slot = AllowedSlots[rng.Next(AllowedSlots.Length)];
        return $"{seed}|{(int)slot}|{playerLevel}";
    }

    /// <summary>
    /// Resolves a recipe string into a <see cref="DynamicEquipmentSave"/>.
    /// Same seed + playerLevel always produces the same item.
    /// </summary>
    public static DynamicEquipmentSave Resolve(string recipe)
    {
        var parts = recipe.Split('|');
        int seed  = int.Parse(parts[0]);
        var slot  = (EquipmentSlot)int.Parse(parts[1]);
        int level = parts.Length > 2 ? int.Parse(parts[2]) : 1;
        var rng   = new Random(seed);

        // Consume one int to stay in sync with GenerateRecipe's slot pick
        _ = rng.Next();

        string prefix   = Pick(rng, Prefixes);
        string material = Pick(rng, Materials);
        string typeName = Pick(rng, SlotNames[slot]);

        int budget = rng.Next(6, 18) + (int)(level * 1.8f);

        int atk = 0, def = 0, mag = 0, hp = 0, spd = 0, lck = 0;
        AllocateBonuses(rng, slot, budget, ref atk, ref def, ref mag, ref hp, ref spd, ref lck);

        return new DynamicEquipmentSave
        {
            Id          = $"kriora_{seed}",
            DisplayName = $"{prefix} {material} {typeName}",
            Description = "Found by Kriora. Resonates with crystalline energy.",
            Slot        = slot,
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
                {
                    // 60% Magic, 20% Speed, 20% Luck
                    int r = rng.Next(5);
                    if (r < 3) mag++;
                    else if (r == 3) spd++;
                    else lck++;
                    break;
                }
                case EquipmentSlot.Accessory:
                {
                    // 40% Magic, 30% Resistance → stored as def, 30% Luck
                    int r = rng.Next(10);
                    if (r < 4) mag++;
                    else if (r < 7) def++;
                    else lck++;
                    break;
                }
                default:
                    mag++;
                    break;
            }
            budget--;
        }
    }
}
