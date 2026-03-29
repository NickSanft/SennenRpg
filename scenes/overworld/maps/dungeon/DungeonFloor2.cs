using Godot;

namespace SennenRpg.Scenes.Overworld;

/// <summary>
/// Dungeon Floor 2 — middle level.
/// Both stairs use MapExit nodes (AutoTrigger = true) set up in the scene.
/// StairsUp returns to Floor 1, StairsDown descends to Floor 3.
/// </summary>
[Tool]
public partial class DungeonFloor2 : OverworldBase
{
    public override void _Ready()
    {
        base._Ready();
    }
}
