using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Replaces Pattern004 (Dustmote chaos / bouncing sparks).
/// Spawns one obstacle in a random lane on every beat, plus an extra
/// obstacle on off-beats (half-beat, tracked via _Process).
/// High tempo, low pattern — rewards reflexes over reading.
/// </summary>
public partial class RandomLanePattern : RhythmPatternBase
{
    private bool _halfBeatFired;

    public override void _Process(double delta)
    {
        if (Arena == null) return;

        float phase = RhythmClock.Instance.BeatPhase;

        // Fire once when phase crosses 0.5 (the "and" of the beat)
        if (phase >= 0.5f && !_halfBeatFired)
        {
            _halfBeatFired = true;
            SpawnObstacle(0);
        }
        if (phase < 0.5f)
            _halfBeatFired = false;
    }

    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        SpawnObstacle(0);
    }
}
