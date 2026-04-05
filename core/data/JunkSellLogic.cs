using System.Collections.Generic;
using System.Linq;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure junk-selling logic — no Godot runtime dependency.
/// All methods are static and side-effect-free so they can be unit-tested with NUnit.
/// </summary>
public static class JunkSellLogic
{
    /// <summary>Returns the total count of junk items.</summary>
    public static int CountJunkItems(IEnumerable<(string Path, int SellValue)> items)
        => items.Count();

    /// <summary>Returns the total gold value of all junk items.</summary>
    public static int TotalJunkValue(IEnumerable<(string Path, int SellValue)> items)
        => items.Sum(i => i.SellValue);
}
