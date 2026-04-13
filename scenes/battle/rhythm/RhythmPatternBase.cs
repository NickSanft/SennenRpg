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
    private bool _alignedToMeasure;

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

    /// <summary>Obstacle density multiplier from the arena's Rhythm Memory adaptation.</summary>
    protected float DensityMult => Arena.ObstacleDensityMult;

    /// <summary>
    /// Lanes spawned into on the current beat. Patterns call <see cref="SpawnObstacle"/>
    /// which records the lane here; the density bonus avoids these lanes so two notes
    /// never overlap in the same lane on the same beat.
    /// </summary>
    private readonly bool[] _lanesUsedThisBeat = new bool[4];

    private void OnBeatInternal(int absoluteBeat)
    {
        if (_finished) return;

        // Wait for the start of a measure before spawning anything,
        // so notes always land on the music's beat grid.
        if (!_alignedToMeasure)
        {
            if (RhythmClock.Instance.BeatInMeasure != 0) return;
            _alignedToMeasure = true;
        }

        int beatInMeasure = _beatsSinceStart % RhythmConstants.BeatsPerMeasure;

        // Cocky enemies skip some beats entirely (density < 1.0)
        if (DensityMult < 1.0f && GD.Randf() > DensityMult)
        {
            _beatsSinceStart++;
            FinishIfDone();
            return;
        }

        // Reset per-beat lane tracking
        _lanesUsedThisBeat[0] = false;
        _lanesUsedThisBeat[1] = false;
        _lanesUsedThisBeat[2] = false;
        _lanesUsedThisBeat[3] = false;

        SpawnOnBeat(beatInMeasure, _beatsSinceStart);

        // Adapted enemies spawn extra obstacles (density > 1.0).
        // Pick a lane that wasn't already used this beat to avoid
        // two overlapping notes the player can't both hit.
        if (DensityMult > 1.0f && GD.Randf() < (DensityMult - 1.0f))
        {
            int freeLane = PickFreeLane();
            if (freeLane >= 0)
                Arena.CreateObstacle(freeLane, RhythmArena.BeatsUntilArrival, 1);
        }

        _beatsSinceStart++;
        FinishIfDone();
    }

    /// <summary>Returns a random lane not used this beat, or -1 if all lanes are taken.</summary>
    private int PickFreeLane()
    {
        // Count free lanes
        int freeCount = 0;
        for (int i = 0; i < 4; i++)
            if (!_lanesUsedThisBeat[i]) freeCount++;

        if (freeCount == 0) return -1;

        // Pick the Nth free lane
        int pick = (int)(GD.Randf() * freeCount);
        for (int i = 0; i < 4; i++)
        {
            if (_lanesUsedThisBeat[i]) continue;
            if (pick == 0) return i;
            pick--;
        }
        return -1; // unreachable
    }

    private void FinishIfDone()
    {
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
    {
        if (lane >= 0 && lane < 4)
            _lanesUsedThisBeat[lane] = true;
        Arena.CreateObstacle(lane, beatsUntilArrival, damage);
    }
}
