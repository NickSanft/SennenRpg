using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Replaces Pattern005 (Stonecrawler column volley).
/// Three consecutive lanes are active; one "gap" lane (changes every measure).
/// Spawns once per beat on beats 0 and 2.
/// Encourages reading ahead and lane-switching between measures.
/// </summary>
public partial class ThreeLanesPattern : RhythmPatternBase
{
    private int _gapLane;

    public override void _Ready()
    {
        base._Ready();
        _gapLane = (int)GD.RandRange(0, 3);
    }

    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        if (beatInMeasure != 0 && beatInMeasure != 2) return;

        SpawnObstacle(0);
    }
}
