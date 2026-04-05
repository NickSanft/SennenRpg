namespace SennenRpg.Core.Data;

/// <summary>
/// A single entry in the forage loot table: an item resource path and a relative weight.
/// </summary>
public readonly record struct ForageTableEntry(string ItemPath, int Weight);
