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
        SpawnObstacle(0);
    }
}
