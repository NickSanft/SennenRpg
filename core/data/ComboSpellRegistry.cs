using System;
using System.Linq;

namespace SennenRpg.Core.Data;

public static class ComboSpellRegistry
{
    public static readonly ComboSpell[] All =
    {
        new("resonance_burst", "Resonance Burst", "Lily", "Sen", 8, 8, ComboSpellType.Magical),
        new("gravity_volley",  "Gravity Volley",  "Bhata", "Rain", 6, 6, ComboSpellType.Physical),
        new("storm_chord",     "Storm Chord",     "Rain", "Sen", 6, 6, ComboSpellType.Hybrid),
        new("crystal_bloom",   "Crystal Bloom",   "Kriora", "Lily", 10, 8, ComboSpellType.Magical),
    };

    /// <summary>
    /// Look up a combo spell by the two member IDs (order does not matter).
    /// </summary>
    public static ComboSpell? Find(string a, string b)
    {
        var (na, nb) = ComboSpellLogic.NormalizePair(a, b);
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].MemberA == na && All[i].MemberB == nb)
                return All[i];
        }
        return null;
    }
}
