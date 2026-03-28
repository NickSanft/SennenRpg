using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Three of four lanes are active each measure; the safe lane rotates.
/// Spawns on strong beats (0 and 2). Player must track which lane is clear.
/// </summary>
public partial class LaneSweepPattern : RhythmPatternBase
{
    private int _safeLane;

    public override void _Ready()
    {
        base._Ready();
        _safeLane = (int)GD.RandRange(0, 3);
    }

    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        if (beatInMeasure != 0 && beatInMeasure != 2) return;

        // Rotate safe lane each measure (every 4 beats)
        if (beatInMeasure == 0 && totalBeat > 0)
            _safeLane = (_safeLane + 1) % 4;

        for (int lane = 0; lane < 4; lane++)
        {
            if (lane == _safeLane) continue;
            SpawnObstacle(lane);
        }
    }
}
