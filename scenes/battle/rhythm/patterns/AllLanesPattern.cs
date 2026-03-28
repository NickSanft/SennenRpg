using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Spawns obstacles in all four lanes simultaneously on the downbeat.
/// Visual warning: every lane lights up at once. High-pressure burst pattern.
/// </summary>
public partial class AllLanesPattern : RhythmPatternBase
{
    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        if (beatInMeasure != 0) return;

        for (int lane = 0; lane < 4; lane++)
            SpawnObstacle(lane, damage: 2);
    }
}
