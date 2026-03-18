using Godot;
using SennenRpg.Autoloads;
using SennenRpg.Core.Data;

namespace SennenRpg.Scenes.Battle;

/// <summary>
/// Base class for all rhythm attack patterns.
/// Subclasses override SpawnOnBeat() to define which lanes receive obstacles
/// on each beat within a measure.
///
/// Lifecycle:
///   1. RhythmArena instantiates the pattern scene.
///   2. Calls pattern.Initialize(arena, measures) BEFORE AddChild.
///   3. Adds the pattern to ObstacleContainer — _Ready fires, subscribes to Beat.
///   4. After TotalMeasures × BeatsPerMeasure beats, emits PatternFinished.
/// </summary>
public abstract partial class RhythmPatternBase : Node
{
    [Signal] public delegate void PatternFinishedEventHandler();

    /// <summary>The arena that owns this pattern.</summary>
    protected RhythmArena Arena { get; private set; } = null!;
    /// <summary>Total measures this pattern runs before emitting PatternFinished.</summary>
    protected int TotalMeasures { get; private set; } = 2;

    private int  _beatsSinceStart;
    private bool _initialized;
    private bool _finished;

    /// <summary>Called by RhythmArena before AddChild — sets the arena reference.</summary>
    public void Initialize(RhythmArena arena, int totalMeasures)
    {
        Arena         = arena;
        TotalMeasures = totalMeasures;
        _initialized  = true;
    }

    public override void _Ready()
    {
        if (!_initialized)
            GD.PushWarning($"[{GetType().Name}] Initialize() was not called before AddChild.");
        RhythmClock.Instance.Beat += OnBeatInternal;
    }

    public override void _ExitTree()
    {
        RhythmClock.Instance.Beat -= OnBeatInternal;
    }

    private void OnBeatInternal(int absoluteBeat)
    {
        if (_finished) return;

        int beatInMeasure = _beatsSinceStart % RhythmConstants.BeatsPerMeasure;
        SpawnOnBeat(beatInMeasure, _beatsSinceStart);
        _beatsSinceStart++;

        if (_beatsSinceStart >= TotalMeasures * RhythmConstants.BeatsPerMeasure)
        {
            _finished = true;
            RhythmClock.Instance.Beat -= OnBeatInternal;
            EmitSignal(SignalName.PatternFinished);
        }
    }

    /// <summary>
    /// Called once per beat. Override to spawn obstacles.
    /// beatInMeasure: 0–3 (position within the current measure).
    /// totalBeat: cumulative beats since pattern started (0-indexed).
    /// </summary>
    protected abstract void SpawnOnBeat(int beatInMeasure, int totalBeat);

    /// <summary>Convenience helper — spawn a StandardObstacle in the given lane.</summary>
    protected void SpawnObstacle(int lane, int damage = 1, int beatsUntilArrival = RhythmArena.BeatsUntilArrival)
        => Arena.CreateObstacle(lane, beatsUntilArrival, damage);
}
