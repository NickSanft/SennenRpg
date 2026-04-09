using System.Collections.Generic;
using Godot;
using SennenRpg.Scenes.Fx;

namespace SennenRpg.Autoloads;

/// <summary>
/// Single subscriber to <see cref="RhythmClock.Beat"/> for the sprite beat-sync
/// system. <see cref="BeatSyncTrigger"/> Nodes register themselves at _Ready and
/// deregister at _ExitTree; the registry walks all of them on each beat (Snap
/// mode) and re-applies SpeedScale on BPM change (Scaled mode).
///
/// When <see cref="RhythmClock.Bpm"/> drops to ≤ 0 (track confidence too low,
/// no BGM, or free-running with default BPM), every registered trigger is told
/// to <see cref="BeatSyncTrigger.RestoreNative"/> and the registry stops touching
/// them until BPM returns. Identical fallback pattern to the rhythm minigames.
/// </summary>
public partial class BeatSyncRegistry : Node
{
    public static BeatSyncRegistry? Instance { get; private set; }

    private readonly HashSet<BeatSyncTrigger> _triggers = new();
    private float _lastAppliedBpm  = -1f;
    private bool  _isRestoredState;

    public override void _Ready()
    {
        Instance    = this;
        ProcessMode = ProcessModeEnum.Always;
        if (RhythmClock.Instance != null)
            RhythmClock.Instance.Beat += OnBeat;
    }

    public override void _ExitTree()
    {
        if (RhythmClock.Instance != null)
            RhythmClock.Instance.Beat -= OnBeat;
    }

    public void Register(BeatSyncTrigger trigger)
    {
        _triggers.Add(trigger);
        // Apply current scale immediately so newly-spawned sprites snap into
        // the right speed instead of waiting for the next BPM change.
        var rc = RhythmClock.Instance;
        if (rc != null && rc.Bpm > 0f)
            trigger.ApplyScale(rc.Bpm);
    }

    public void Unregister(BeatSyncTrigger trigger) => _triggers.Remove(trigger);

    /// <summary>
    /// Force a single trigger to re-apply its scale. Used by
    /// <see cref="BeatSyncTrigger.SetUserMultiplier"/> so the running multiplier
    /// takes effect on the same frame instead of waiting for a BPM change.
    /// </summary>
    public void RefreshTrigger(BeatSyncTrigger trigger)
    {
        var rc = RhythmClock.Instance;
        if (rc == null) return;
        if (rc.Bpm > 0f) trigger.ApplyScale(rc.Bpm);
        else             trigger.RestoreNative();
    }

    public override void _Process(double delta)
    {
        var rc = RhythmClock.Instance;
        if (rc == null) return;

        float bpm = rc.Bpm;

        // BPM dropped below the floor: restore everything to native and idle.
        if (bpm <= 0f)
        {
            if (!_isRestoredState)
            {
                foreach (var t in _triggers) t.RestoreNative();
                _isRestoredState = true;
                _lastAppliedBpm  = -1f;
            }
            return;
        }

        // Re-apply scaled mode any time the live BPM changes meaningfully.
        if (_isRestoredState || Mathf.Abs(bpm - _lastAppliedBpm) > 0.01f)
        {
            foreach (var t in _triggers) t.ApplyScale(bpm);
            _lastAppliedBpm  = bpm;
            _isRestoredState = false;
        }
    }

    private void OnBeat(int beatIndex)
    {
        // Snap mode is event-driven on every beat. Scaled mode is handled in _Process.
        foreach (var t in _triggers)
            t.HandleBeat(beatIndex);
    }
}
