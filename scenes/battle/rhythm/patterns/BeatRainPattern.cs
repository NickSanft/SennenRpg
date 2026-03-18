using Godot;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Replaces Pattern001 (PatternRandom). Spawns 1–2 random lane obstacles
/// on every beat of the measure. Chaotic but readable — the equivalent of
/// a random bullet rain converted to lane-based rhythm.
/// </summary>
public partial class BeatRainPattern : RhythmPatternBase
{
    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        // Spawn 1 obstacle, occasionally 2 on strong beats (0 and 2)
        int count = (beatInMeasure == 0 || beatInMeasure == 2) && GD.RandRange(0, 1) == 0 ? 2 : 1;

        var used = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < count; i++)
        {
            int lane;
            int tries = 0;
            do
            {
                lane = (int)GD.RandRange(0, 3);
                tries++;
            }
            while (used.Contains(lane) && tries < 8);

            used.Add(lane);
            SpawnObstacle(lane);
        }
    }
}
