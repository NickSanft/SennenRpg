using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Off-beat (half-beat) only pattern with an alternating cross-lane sequence.
/// Rewards internalising the rhythm rather than just reacting to visuals.
/// </summary>
public partial class SyncopatedPattern : RhythmPatternBase
{
    private bool  _halfBeatFired;
    private int   _halfBeatCount;
    private int[] _laneSequence = null!;

    public override void _Ready()
    {
        base._Ready();
        _laneSequence = new[] { 0, 3, 1, 2, 3, 0, 2, 1 };
    }

    public override void _Process(double delta)
    {
        if (Arena == null) return;

        float phase = RhythmClock.Instance.BeatPhase;

        if (phase >= 0.5f && !_halfBeatFired)
        {
            _halfBeatFired = true;
            int lane = _laneSequence[_halfBeatCount % _laneSequence.Length];
            SpawnObstacle(lane);
            _halfBeatCount++;
        }
        if (phase < 0.5f)
            _halfBeatFired = false;
    }

    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        // All spawning happens on the half-beat in _Process
    }
}
