using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Replaces Pattern006 (Flickerfly zigzag).
/// Spawns obstacles on the OFF-beat (half-beat) rather than on the beat,
/// creating syncopation. Also alternates between two specific lanes per
/// measure to create a cross-pattern. Rewards good rhythm internalization.
/// </summary>
public partial class SyncopatedPattern : RhythmPatternBase
{
    private bool  _halfBeatFired;
    private int   _halfBeatCount;
    private int[] _laneSequence = null!;

    public override void _Ready()
    {
        base._Ready();
        // Alternating cross-lane sequence: 0↔3, 1↔2
        _laneSequence = new[] { 0, 3, 1, 2, 3, 0, 2, 1 };
    }

    public override void _Process(double delta)
    {
        if (Arena == null) return;

        float phase = RhythmClock.Instance.BeatPhase;

        if (phase >= 0.5f && !_halfBeatFired)
        {
            _halfBeatFired = true;
            SpawnObstacle(0);
            _halfBeatCount++;
        }
        if (phase < 0.5f)
            _halfBeatFired = false;
    }

    protected override void SpawnOnBeat(int beatInMeasure, int totalBeat)
    {
        // No on-beat spawns for this pattern — only off-beat (handled in _Process)
    }
}
