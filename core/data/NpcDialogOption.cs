using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// A single conditional dialog branch for an NPC.
/// The NPC plays this timeline when RequiredFlag is set in GameManager.
///
/// Use with Npc.SelectTimeline or manually to represent a conditional dialog branch.
/// Options are checked in order — the first one whose flag is set wins.
/// If none match, the Npc's default TimelinePath is used.
///
/// Use flag name constants from <see cref="Flags"/> for RequiredFlag to prevent typos.
/// </summary>
[GlobalClass]
public partial class NpcDialogOption : Resource
{
    /// <summary>
    /// GameManager flag that must be true for this dialog branch to activate.
    /// Use a constant from <see cref="Flags"/> (e.g. Flags.MetShizu).
    /// </summary>
    [Export] public string RequiredFlag { get; set; } = "";

    /// <summary>Res:// path to the .dtl timeline to play when RequiredFlag is set.</summary>
    [Export] public string TimelinePath { get; set; } = "";
}
