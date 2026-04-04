using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// Data resource defining a castable spell.
/// Place instances in res://resources/spells/.
/// </summary>
[GlobalClass]
public partial class SpellData : Resource
{
    [Export] public string SpellId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    /// <summary>Raw power before Magic scaling. Used by BattleFormulas.MagicDamage.</summary>
    [Export] public int BasePower { get; set; } = 10;
    [Export] public int MpCost { get; set; } = 8;
    [Export] public Texture2D? Icon { get; set; }
    /// <summary>Optional custom minigame scene. Null uses the default ShadowBoltMinigame.</summary>
    [Export] public PackedScene? MinigameScene { get; set; }
    /// <summary>When true, this spell can be cast from the overworld Spells menu (e.g., Teleport).</summary>
    [Export] public bool OverworldUsable { get; set; } = false;
    /// <summary>Optional scene path to transition to when cast in overworld (e.g., for teleport spells).</summary>
    [Export] public string OverworldTargetScene { get; set; } = "";
}
