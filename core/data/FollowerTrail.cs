using System.Collections.Generic;
using Godot;

namespace SennenRpg.Core.Data;

/// <summary>
/// Pure ring-buffer of recent leader positions used by the overworld follower system.
/// Each call to <see cref="Push"/> records the leader's tile-snapped position so a follower
/// indexed N steps back can read where the leader was N steps ago — producing the
/// classic Dragon Warrior 3 / Phantasy Star line of followers.
///
/// No Godot runtime dependency beyond <see cref="Vector2"/>; fully NUnit-testable.
/// </summary>
public class FollowerTrail
{
    private readonly List<Vector2> _positions = new();
    private readonly int _capacity;

    /// <param name="capacity">
    /// Maximum number of past positions to retain. Must be at least the maximum number
    /// of followers in the chain so the deepest follower has a real entry to read from.
    /// </param>
    public FollowerTrail(int capacity = 8)
    {
        if (capacity < 1) capacity = 1;
        _capacity = capacity;
    }

    public int Count => _positions.Count;

    /// <summary>True when no positions have been pushed yet.</summary>
    public bool IsEmpty => _positions.Count == 0;

    /// <summary>
    /// Append a new leader position. When the ring buffer overflows, the oldest entry
    /// is dropped so the newest <see cref="Capacity"/> positions are always retained.
    /// </summary>
    public void Push(Vector2 leaderPosition)
    {
        _positions.Add(leaderPosition);
        while (_positions.Count > _capacity)
            _positions.RemoveAt(0);
    }

    /// <summary>
    /// Returns the leader's position from <paramref name="stepsBack"/> steps ago.
    /// 1 = most recent push, 2 = the one before that, etc. When the buffer is too short
    /// (e.g. the leader has only just started moving) the oldest available entry is returned.
    /// When the buffer is empty, returns <paramref name="fallback"/> so the caller can
    /// stand idle on the spawn tile.
    /// </summary>
    public Vector2 GetTrailPosition(int stepsBack, Vector2 fallback)
    {
        if (_positions.Count == 0) return fallback;
        if (stepsBack < 1) stepsBack = 1;
        int idx = _positions.Count - stepsBack;
        if (idx < 0) return _positions[0];
        return _positions[idx];
    }

    /// <summary>Resets the buffer. Used when a new map loads.</summary>
    public void Clear() => _positions.Clear();

    /// <summary>Capacity the trail was constructed with.</summary>
    public int Capacity => _capacity;
}
