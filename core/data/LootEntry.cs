using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// One entry in a chest's loot table.
/// Use <see cref="LootLogic.RollLoot"/> to select from a table of these.
/// Export arrays of <c>Resource[]</c> (not <c>LootEntry[]</c>) per Godot 4 C# sub-resource rules;
/// cast with <c>OfType&lt;LootEntry&gt;()</c> at runtime.
/// </summary>
[GlobalClass]
public partial class LootEntry : Resource
{
    /// <summary>Path to the <see cref="ItemData"/> resource to award.</summary>
    [Export] public string ItemPath { get; set; } = "";

    /// <summary>
    /// Relative probability weight used by the weighted roll.
    /// Higher values are more likely. Ignored when <see cref="Guaranteed"/> is true.
    /// </summary>
    [Export] public int Weight { get; set; } = 1;

    /// <summary>
    /// When true this entry is always awarded and the weighted roll is skipped entirely.
    /// Use for quest-critical items that must always appear.
    /// </summary>
    [Export] public bool Guaranteed { get; set; } = false;
}
