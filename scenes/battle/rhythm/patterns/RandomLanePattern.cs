using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// One random-lane note on every beat, plus a second random note on the half-beat.
/// High tempo, rewards reflexes over pattern-reading.
/// </summary>
public partial class RandomLanePattern : RhythmPatternBase
{
    private bool _halfBeatFired;

    public override void _Process(double delta)
    {
        if (Arena == null) return;

        float phase = RhythmClock.Instance.BeatPhase;

        if (phase >= 0.5f && !_halfBeatFired)
        {
            _halfBeatFired = true;
            SpawnObstacle((int)GD.RandRange(0, 3));
        }
        if (phase < 0.5f)
            _halfBeatFired = false;
    }

    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        SpawnObstacle((int)GD.RandRange(0, 3));
    }
}
