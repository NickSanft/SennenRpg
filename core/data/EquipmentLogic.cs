using System.Collections.Generic;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure static equipment logic — no Godot runtime required (fully unit-testable).
/// </summary>
public static class EquipmentLogic
{
    /// <summary>
    /// Sums all equipment bonuses from a collection of equipped items.
    /// Pass <c>EquipmentData.Bonuses</c> for each occupied slot.
    /// </summary>
    public static EquipmentBonuses SumBonuses(IEnumerable<EquipmentBonuses> equipped)
    {
        int hp = 0, atk = 0, def = 0, mag = 0, res = 0, spd = 0, lck = 0;
        foreach (var b in equipped)
        {
            hp  += b.MaxHp;
            atk += b.Attack;
            def += b.Defense;
            mag += b.Magic;
            res += b.Resistance;
            spd += b.Speed;
            lck += b.Luck;
        }
        return new EquipmentBonuses(hp, atk, def, mag, res, spd, lck);
    }
}
