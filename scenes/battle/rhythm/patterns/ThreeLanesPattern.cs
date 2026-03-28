using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Three consecutive lanes are active; one gap lane changes every measure.
/// Spawns on beats 0 and 2. Encourages reading ahead and lane-switching.
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

        // Shift gap lane every measure
        if (beatInMeasure == 0 && totalBeat > 0)
            _gapLane = (_gapLane + 1) % 4;

        for (int lane = 0; lane < 4; lane++)
        {
            if (lane == _gapLane) continue;
            SpawnObstacle(lane);
        }
    }
}
