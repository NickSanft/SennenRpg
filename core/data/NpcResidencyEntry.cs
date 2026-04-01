using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// Defines one purchasable NPC resident slot sold by Rork in Mellyr Outpost.
/// Assign in the inspector on RorkTownNpc's ResidencyStock array.
/// </summary>
[GlobalClass]
public partial class NpcResidencyEntry : Resource
{
    /// <summary>Name shown in the residency shop (e.g. "Rain").</summary>
    [Export] public string DisplayName { get; set; } = "";

    /// <summary>Flavour text shown below the name in the shop row.</summary>
    [Export] public string Description { get; set; } = "";

    /// <summary>
    /// GameManager flag key that tracks whether this resident has been purchased.
    /// Use Flags constants (e.g. Flags.NpcRainPurchased).
    /// </summary>
    [Export] public string FlagKey { get; set; } = "";

    /// <summary>Gold cost to hire this resident.</summary>
    [Export] public int Price { get; set; } = 0;
}
