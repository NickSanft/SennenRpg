using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Spawns 1–2 random-lane obstacles on every beat.
/// Chaotic but readable — the rhythm equivalent of bullet rain.
/// </summary>
public partial class BeatRainPattern : RhythmPatternBase
{
    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        int lane1 = (int)GD.RandRange(0, 3);
        SpawnObstacle(lane1);

        // 50 % chance of a second note in a different lane
        if (GD.Randf() > 0.5f)
        {
            int lane2 = (lane1 + 1 + (int)GD.RandRange(0, 2)) % 4;
            SpawnObstacle(lane2);
        }
    }
}
