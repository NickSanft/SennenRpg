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

    // ── Party recruitment (Phase 3) ───────────────────────────────────

    /// <summary>
    /// Stable lowercase party-member id for the recruited NPC (e.g. "lily", "rain").
    /// Empty when this entry is a passive resident only (Mellyr cottages, etc.) and
    /// does NOT join the active party. When set, hiring also calls
    /// <c>GameManager.RecruitPartyMember</c>.
    /// </summary>
    [Export] public string PartyMemberId { get; set; } = "";

    /// <summary>The class the recruited member is locked into.</summary>
    [Export] public PlayerClass JoinClass { get; set; } = PlayerClass.Bard;

    /// <summary>
    /// CharacterStats template defining the recruit's starting stats. Typed as
    /// <see cref="Resource"/> per the CLAUDE.md gotcha — cast to CharacterStats at runtime.
    /// </summary>
    [Export] public Resource? StartingStats { get; set; }

    /// <summary>
    /// Overworld follower sprite path used by the Phase 4 follower system on 16×16 maps.
    /// Stored on the recruit's PartyMember at hire time.
    /// </summary>
    [Export] public string OverworldSpritePath { get; set; } = "";

    /// <summary>
    /// Optional Dialogic timeline path played after the residency menu closes when this
    /// entry has just been recruited. Empty for silent recruits.
    /// </summary>
    [Export] public string JoinTimelinePath { get; set; } = "";
}
